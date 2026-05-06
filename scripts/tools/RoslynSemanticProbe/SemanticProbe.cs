using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace QudJP.Tools.RoslynSemanticProbe;

internal static class SemanticProbe
{
    private const string SchemaVersion = "1.0";
    private static readonly SymbolDisplayFormat TypeFormat = SymbolDisplayFormat.CSharpErrorMessageFormat;

    public static ProbeResult Run(CliOptions options)
    {
        var total = Stopwatch.StartNew();
        var sourceRoot = Path.GetFullPath(options.SourceRoot);
        var methodPrefix = options.Method?.EndsWith('*') == true ? options.Method[..^1] : options.Method;
        var isPrefix = options.Method?.EndsWith('*') == true;
        var queryToken = options.AssignmentProperty ?? methodPrefix ?? "";

        var enumerateWatch = Stopwatch.StartNew();
        var files = Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
            .Select(path => new SourcePath(sourceRoot, path))
            .Where(source => options.PathFilter is null || source.RelativePath.Contains(options.PathFilter, StringComparison.Ordinal))
            .OrderBy(source => source.RelativePath, StringComparer.Ordinal)
            .ToList();
        enumerateWatch.Stop();

        var prefilterWatch = Stopwatch.StartNew();
        var candidatePaths = files
            .AsParallel()
            .Where(source => File.ReadAllText(source.FullPath).Contains(queryToken, StringComparison.Ordinal))
            .OrderBy(source => source.RelativePath, StringComparer.Ordinal)
            .ToList();
        prefilterWatch.Stop();

        var parseWatch = Stopwatch.StartNew();
        var parsed = ParseSources(options.CompileCandidatesOnly ? candidatePaths : files, candidatePaths);
        parseWatch.Stop();

        var compilationWatch = Stopwatch.StartNew();
        var compilation = CSharpCompilation.Create(
            "QudJP.RoslynSemanticProbe.Input",
            parsed.Select(source => source.Tree),
            ReferenceResolver.Resolve(options.ReferencePaths),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));
        compilationWatch.Stop();

        var scanWatch = Stopwatch.StartNew();
        var scan = ScanParsedSources(options, methodPrefix, isPrefix, parsed, compilation);
        scanWatch.Stop();
        total.Stop();

        return new ProbeResult(
            SchemaVersion,
            new QueryInfo(
                options.Method,
                options.AssignmentProperty,
                options.Owners,
                options.PathFilter,
                options.ReferencePaths.Count,
                options.ReferenceSources),
            new ProbeMetrics(
                TotalFiles: files.Count,
                ParsedFiles: parsed.Count,
                CandidateFiles: candidatePaths.Count,
                ReturnedHits: scan.Hits.Count,
                ResolvedMatchingOwnerHits: scan.ResolvedMatchingOwnerHits,
                CandidateMatchingOwnerHits: scan.CandidateMatchingOwnerHits,
                UnresolvedHits: scan.StatusCounts.GetValueOrDefault(SymbolStatus.Unresolved),
                StatusCounts: OrderedCounts(scan.StatusCounts),
                OwnerCounts: scan.OwnerCounts
                    .OrderByDescending(pair => pair.Value)
                    .ThenBy(pair => pair.Key, StringComparer.Ordinal)
                    .Take(20)
                    .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal),
                StringArgumentCounts: OrderedCounts(scan.StringArgumentCounts),
                FirstStringArgumentCounts: OrderedCounts(scan.FirstStringArgumentCounts),
                StringRiskCounts: OrderedCounts(scan.StringRiskCounts),
                TimingsMs: new TimingMetrics(
                    enumerateWatch.ElapsedMilliseconds,
                    prefilterWatch.ElapsedMilliseconds,
                    parseWatch.ElapsedMilliseconds,
                    compilationWatch.ElapsedMilliseconds,
                    scanWatch.ElapsedMilliseconds,
                    total.ElapsedMilliseconds)),
            scan.Hits);
    }

    private static List<SourceFile> ParseSources(IReadOnlyList<SourcePath> pathsToParse, IReadOnlyList<SourcePath> candidatePaths)
    {
        var sourceFiles = new ConcurrentBag<SourceFile>();
        var candidateRelativePaths = candidatePaths
            .Select(path => path.RelativePath)
            .ToHashSet(StringComparer.Ordinal);
        Parallel.ForEach(pathsToParse, source =>
        {
            var text = File.ReadAllText(source.FullPath);
            var tree = CSharpSyntaxTree.ParseText(
                text,
                CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview),
                path: source.RelativePath);
            sourceFiles.Add(new SourceFile(source, tree, candidateRelativePaths.Contains(source.RelativePath)));
        });

        return sourceFiles.OrderBy(source => source.Path.RelativePath, StringComparer.Ordinal).ToList();
    }

    private static ScanAccumulator ScanParsedSources(
        CliOptions options,
        string? methodPrefix,
        bool isPrefix,
        IReadOnlyList<SourceFile> parsed,
        CSharpCompilation compilation)
    {
        var scan = new ScanAccumulator(new HashSet<string>(options.Owners, StringComparer.Ordinal), options.Limit);
        foreach (var source in parsed.Where(source => source.ShouldScan))
        {
            var semanticModel = compilation.GetSemanticModel(source.Tree);
            var root = source.Tree.GetCompilationUnitRoot();
            if (methodPrefix is not null)
            {
                ScanInvocations(options, methodPrefix, isPrefix, source, semanticModel, root, scan);
            }

            if (options.AssignmentProperty is not null)
            {
                ScanAssignments(options, source, semanticModel, root, scan);
            }
        }

        return scan;
    }

    private static void ScanInvocations(
        CliOptions options,
        string methodPrefix,
        bool isPrefix,
        SourceFile source,
        SemanticModel semanticModel,
        CompilationUnitSyntax root,
        ScanAccumulator scan)
    {
        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (!MethodNameMatches(invocation, methodPrefix, isPrefix, out var syntaxMethod))
            {
                continue;
            }

            var (methodSymbol, status) = ResolveInvocationSymbol(semanticModel, invocation);
            var containingType = methodSymbol?.ContainingType?.ToDisplayString(TypeFormat);
            var ownerMatches = scan.Record(status, containingType);
            var stringArguments = ownerMatches
                ? StringExpressionClassifier.AnalyzeInvocationArguments(semanticModel, invocation).ToList()
                : new List<StringArgumentHit>();
            scan.CountStringArguments(stringArguments);

            if (!ownerMatches && !options.IncludeNonMatchingOwners)
            {
                continue;
            }

            scan.AddHitIfAllowed(
                source,
                invocation,
                syntaxMethod,
                invocation.ToString().Trim(),
                status,
                ownerMatches,
                methodSymbol?.ToDisplayString(SymbolFormats.Method),
                containingType,
                ReceiverType(semanticModel, invocation),
                stringArguments);
        }
    }

    private static void ScanAssignments(
        CliOptions options,
        SourceFile source,
        SemanticModel semanticModel,
        CompilationUnitSyntax root,
        ScanAccumulator scan)
    {
        foreach (var assignment in root.DescendantNodes().OfType<AssignmentExpressionSyntax>())
        {
            if (!assignment.IsKind(SyntaxKind.SimpleAssignmentExpression)
                || assignment.Left is not MemberAccessExpressionSyntax memberAccess
                || !memberAccess.Name.Identifier.ValueText.Equals(options.AssignmentProperty, StringComparison.Ordinal))
            {
                continue;
            }

            var (symbol, status) = ResolveMemberSymbol(semanticModel, memberAccess);
            var containingType = symbol?.ContainingType?.ToDisplayString(TypeFormat);
            var ownerMatches = scan.Record(status, containingType);
            var stringArguments = ownerMatches
                ? StringExpressionClassifier.AnalyzeAssignmentExpression(semanticModel, assignment.Right).ToList()
                : new List<StringArgumentHit>();
            scan.CountStringArguments(stringArguments);

            if (!ownerMatches && !options.IncludeNonMatchingOwners)
            {
                continue;
            }

            scan.AddHitIfAllowed(
                source,
                assignment,
                options.AssignmentProperty!,
                assignment.ToString().Trim(),
                status,
                ownerMatches,
                symbol?.ToDisplayString(TypeFormat),
                containingType,
                semanticModel.GetTypeInfo(memberAccess.Expression).Type?.ToDisplayString(TypeFormat),
                stringArguments);
        }
    }

    private static (IMethodSymbol? Symbol, string Status) ResolveInvocationSymbol(
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation)
    {
        var symbolInfo = semanticModel.GetSymbolInfo(invocation);
        if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
        {
            return (methodSymbol, SymbolStatus.Resolved);
        }

        var candidate = symbolInfo.CandidateSymbols
            .OfType<IMethodSymbol>()
            .OrderBy(symbol => symbol.ToDisplayString(SymbolFormats.Method), StringComparer.Ordinal)
            .FirstOrDefault();
        return candidate is null ? (null, SymbolStatus.Unresolved) : (candidate, SymbolStatus.Candidate);
    }

    private static (ISymbol? Symbol, string Status) ResolveMemberSymbol(
        SemanticModel semanticModel,
        MemberAccessExpressionSyntax memberAccess)
    {
        var symbolInfo = semanticModel.GetSymbolInfo(memberAccess);
        if (symbolInfo.Symbol is not null)
        {
            return (symbolInfo.Symbol, SymbolStatus.Resolved);
        }

        var candidate = symbolInfo.CandidateSymbols
            .OrderBy(symbol => symbol.ToDisplayString(TypeFormat), StringComparer.Ordinal)
            .FirstOrDefault();
        return candidate is null ? (null, SymbolStatus.Unresolved) : (candidate, SymbolStatus.Candidate);
    }

    private static bool MethodNameMatches(
        InvocationExpressionSyntax invocation,
        string methodPrefix,
        bool isPrefix,
        out string methodName)
    {
        methodName = invocation.Expression switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText,
            MemberBindingExpressionSyntax memberBinding => memberBinding.Name.Identifier.ValueText,
            _ => "",
        };

        return isPrefix
            ? methodName.StartsWith(methodPrefix, StringComparison.Ordinal)
            : methodName.Equals(methodPrefix, StringComparison.Ordinal);
    }

    private static string? ReceiverType(SemanticModel semanticModel, InvocationExpressionSyntax invocation)
    {
        return invocation.Expression is MemberAccessExpressionSyntax memberAccess
            ? semanticModel.GetTypeInfo(memberAccess.Expression).Type?.ToDisplayString(TypeFormat)
            : null;
    }

    private static IReadOnlyDictionary<string, int> OrderedCounts(Dictionary<string, int> counts)
    {
        return counts
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key, StringComparer.Ordinal)
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
    }
}

internal sealed record SourcePath(string SourceRoot, string FullPath)
{
    public string RelativePath { get; } =
        Path.GetRelativePath(SourceRoot, FullPath).Replace(Path.DirectorySeparatorChar, '/');
}

internal sealed record SourceFile(SourcePath Path, SyntaxTree Tree, bool ShouldScan);

internal sealed class ScanAccumulator
{
    private readonly HashSet<string> owners;
    private readonly int hitLimit;

    public ScanAccumulator(HashSet<string> owners, int hitLimit)
    {
        this.owners = owners;
        this.hitLimit = hitLimit;
    }

    public List<ProbeHit> Hits { get; } = new();
    public Dictionary<string, int> StatusCounts { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, int> OwnerCounts { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, int> StringArgumentCounts { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, int> FirstStringArgumentCounts { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, int> StringRiskCounts { get; } = new(StringComparer.Ordinal);
    public int ResolvedMatchingOwnerHits { get; private set; }
    public int CandidateMatchingOwnerHits { get; private set; }

    public bool Record(string status, string? containingType)
    {
        Increment(StatusCounts, status);
        if (containingType is null)
        {
            return false;
        }

        Increment(OwnerCounts, containingType);
        var ownerMatches = owners.Contains(containingType);
        if (ownerMatches && status == SymbolStatus.Resolved)
        {
            ResolvedMatchingOwnerHits++;
        }
        else if (ownerMatches && status == SymbolStatus.Candidate)
        {
            CandidateMatchingOwnerHits++;
        }

        return ownerMatches;
    }

    public void CountStringArguments(IReadOnlyList<StringArgumentHit> stringArguments)
    {
        foreach (var argument in stringArguments)
        {
            Increment(StringArgumentCounts, argument.ExpressionKind);
            if (argument.HasQudMarkup)
            {
                Increment(StringRiskCounts, "qud_markup");
            }

            if (argument.HasTmpMarkup)
            {
                Increment(StringRiskCounts, "tmp_markup");
            }

            if (argument.HasPlaceholderLikeText)
            {
                Increment(StringRiskCounts, "placeholder_like_text");
            }
        }

        var firstStringArgument = stringArguments.FirstOrDefault();
        if (firstStringArgument is not null)
        {
            Increment(FirstStringArgumentCounts, firstStringArgument.ExpressionKind);
        }
    }

    public void AddHitIfAllowed(
        SourceFile source,
        SyntaxNode node,
        string syntaxMethod,
        string expression,
        string status,
        bool ownerMatches,
        string? methodOrPropertySymbol,
        string? containingType,
        string? receiverType,
        IReadOnlyList<StringArgumentHit> stringArguments)
    {
        if (Hits.Count >= hitLimit)
        {
            return;
        }

        var line = source.Tree.GetLineSpan(node.Span).StartLinePosition.Line + 1;
        Hits.Add(new ProbeHit(
            source.Path.RelativePath,
            line,
            syntaxMethod,
            expression,
            status,
            ownerMatches,
            methodOrPropertySymbol,
            containingType,
            receiverType,
            stringArguments));
    }

    private static void Increment(Dictionary<string, int> counts, string key)
    {
        counts[key] = counts.TryGetValue(key, out var value) ? value + 1 : 1;
    }
}

internal static class SymbolStatus
{
    public const string Resolved = "resolved";
    public const string Candidate = "candidate";
    public const string Unresolved = "unresolved";
}

internal static class SymbolFormats
{
    public static readonly SymbolDisplayFormat Method = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        memberOptions:
            SymbolDisplayMemberOptions.IncludeContainingType
            | SymbolDisplayMemberOptions.IncludeParameters
            | SymbolDisplayMemberOptions.IncludeType,
        parameterOptions:
            SymbolDisplayParameterOptions.IncludeType
            | SymbolDisplayParameterOptions.IncludeName
            | SymbolDisplayParameterOptions.IncludeParamsRefOut,
        miscellaneousOptions:
            SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers
            | SymbolDisplayMiscellaneousOptions.UseSpecialTypes);
}
