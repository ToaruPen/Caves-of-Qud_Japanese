namespace QudJP.Tools.RoslynSemanticProbe;

internal sealed record CliOptions(
    string SourceRoot,
    string? OutputPath,
    string? Method,
    string? AssignmentProperty,
    IReadOnlyList<string> Owners,
    IReadOnlyList<string> ReferencePaths,
    IReadOnlyList<string> ReferenceSources,
    string? PathFilter,
    int Limit,
    bool IncludeNonMatchingOwners,
    bool CompileCandidatesOnly,
    bool ShowHelp,
    string? Error)
{
    public static string HelpText =>
        """
        Usage:
          RoslynSemanticProbe --method <name-or-prefix*> --owner <fully.qualified.Type> [--owner ...]
          RoslynSemanticProbe --assignment-property <property> --owner <fully.qualified.Type> [--owner ...]
                              [--source-root <dir>] [--path-filter <substring>] [--limit <n>]
                              [--managed-dir <dir>] [--reference <dll>] [--reference <dll>]
                              [--include-nonmatching-owners] [--compile-candidates-only]
                              [--output <json-path>]
        """;

    public static CliOptions Parse(IReadOnlyList<string> args)
    {
        var sourceRoot = ExpandHome("~/dev/coq-decompiled_stable");
        string? outputPath = null;
        string? method = null;
        string? assignmentProperty = null;
        string? pathFilter = null;
        var owners = new List<string>();
        var referencePaths = new List<string>();
        var referenceSources = new List<string>();
        var managedDirs = new List<string>();
        var limit = 20;
        var includeNonMatchingOwners = false;
        var compileCandidatesOnly = false;

        for (var index = 0; index < args.Count; index++)
        {
            switch (args[index])
            {
                case "--help":
                case "-h":
                    return Empty() with { ShowHelp = true };
                case "--source-root":
                    if (!TryReadValue(args, ref index, out sourceRoot))
                    {
                        return ErrorConfig("missing value for --source-root");
                    }

                    sourceRoot = ExpandHome(sourceRoot);
                    break;
                case "--output":
                    if (!TryReadValue(args, ref index, out outputPath))
                    {
                        return ErrorConfig("missing value for --output");
                    }

                    outputPath = ExpandHome(outputPath);
                    break;
                case "--method":
                    if (!TryReadValue(args, ref index, out method))
                    {
                        return ErrorConfig("missing value for --method");
                    }

                    break;
                case "--assignment-property":
                    if (!TryReadValue(args, ref index, out assignmentProperty))
                    {
                        return ErrorConfig("missing value for --assignment-property");
                    }

                    break;
                case "--owner":
                    if (!TryReadValue(args, ref index, out var owner))
                    {
                        return ErrorConfig("missing value for --owner");
                    }

                    owners.Add(owner);
                    break;
                case "--reference":
                    if (!TryReadValue(args, ref index, out var referencePath))
                    {
                        return ErrorConfig("missing value for --reference");
                    }

                    referencePaths.Add(ExpandHome(referencePath));
                    referenceSources.Add("explicit");
                    break;
                case "--managed-dir":
                    if (!TryReadValue(args, ref index, out var managedDir))
                    {
                        return ErrorConfig("missing value for --managed-dir");
                    }

                    managedDirs.Add(ExpandHome(managedDir));
                    break;
                case "--path-filter":
                    if (!TryReadValue(args, ref index, out pathFilter))
                    {
                        return ErrorConfig("missing value for --path-filter");
                    }

                    break;
                case "--limit":
                    if (!TryReadValue(args, ref index, out var limitValue) || !int.TryParse(limitValue, out limit))
                    {
                        return ErrorConfig("invalid value for --limit");
                    }

                    break;
                case "--include-nonmatching-owners":
                    includeNonMatchingOwners = true;
                    break;
                case "--compile-candidates-only":
                    compileCandidatesOnly = true;
                    break;
                default:
                    return ErrorConfig($"unknown argument: {args[index]}");
            }
        }

        var validationError = ValidateQuery(method, assignmentProperty, owners);
        if (validationError is not null)
        {
            return ErrorConfig(validationError);
        }

        foreach (var managedDir in managedDirs)
        {
            if (!Directory.Exists(managedDir))
            {
                return ErrorConfig($"managed dir does not exist: {managedDir}");
            }

            referencePaths.AddRange(Directory.EnumerateFiles(managedDir, "Unity*.dll", SearchOption.TopDirectoryOnly));
            referenceSources.Add($"managed-dir:{Path.GetFullPath(managedDir)}");
        }

        var normalizedReferences = referencePaths
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();
        var missingReference = normalizedReferences.FirstOrDefault(path => !File.Exists(path));
        if (missingReference is not null)
        {
            return ErrorConfig($"reference does not exist: {missingReference}");
        }

        return new CliOptions(
            sourceRoot,
            outputPath,
            method,
            assignmentProperty,
            owners,
            normalizedReferences,
            referenceSources.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList(),
            pathFilter,
            Math.Max(0, limit),
            includeNonMatchingOwners,
            compileCandidatesOnly,
            ShowHelp: false,
            Error: null);
    }

    private static string? ValidateQuery(string? method, string? assignmentProperty, IReadOnlyList<string> owners)
    {
        if (string.IsNullOrWhiteSpace(method) && string.IsNullOrWhiteSpace(assignmentProperty))
        {
            return "missing required argument: --method or --assignment-property";
        }

        if (!string.IsNullOrWhiteSpace(method) && !string.IsNullOrWhiteSpace(assignmentProperty))
        {
            return "use either --method or --assignment-property, not both";
        }

        return owners.Count == 0 ? "missing required argument: --owner" : null;
    }

    private static CliOptions Empty() =>
        new(
            "",
            null,
            null,
            null,
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            null,
            20,
            false,
            false,
            ShowHelp: false,
            Error: null);

    private static CliOptions ErrorConfig(string error) => Empty() with { Error = error };

    private static bool TryReadValue(IReadOnlyList<string> args, ref int index, out string value)
    {
        if (index + 1 >= args.Count)
        {
            value = "";
            return false;
        }

        value = args[++index];
        return true;
    }

    private static string ExpandHome(string path)
    {
        if (path == "~")
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        return path.StartsWith("~/", StringComparison.Ordinal)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), path[2..])
            : path;
    }
}
