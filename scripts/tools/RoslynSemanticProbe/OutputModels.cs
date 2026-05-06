using System.Text.Json.Serialization;

namespace QudJP.Tools.RoslynSemanticProbe;

internal sealed record ProbeResult(
    string SchemaVersion,
    QueryInfo Query,
    ProbeMetrics Metrics,
    IReadOnlyList<ProbeHit> Hits);

internal sealed record QueryInfo(
    string? Method,
    string? AssignmentProperty,
    IReadOnlyList<string> Owners,
    string? PathFilter,
    int ExternalReferenceCount,
    IReadOnlyList<string> ReferenceSources);

internal sealed record ProbeMetrics(
    int TotalFiles,
    int ParsedFiles,
    int CandidateFiles,
    int ReturnedHits,
    int ResolvedMatchingOwnerHits,
    int CandidateMatchingOwnerHits,
    int UnresolvedHits,
    IReadOnlyDictionary<string, int> StatusCounts,
    IReadOnlyDictionary<string, int> OwnerCounts,
    IReadOnlyDictionary<string, int> StringArgumentCounts,
    IReadOnlyDictionary<string, int> FirstStringArgumentCounts,
    IReadOnlyDictionary<string, int> StringRiskCounts,
    TimingMetrics TimingsMs);

internal sealed record TimingMetrics(long Enumerate, long Prefilter, long Parse, long Compilation, long Scan, long Total);

internal sealed record ProbeHit(
    string File,
    int Line,
    string SyntaxMethod,
    string Expression,
    string RoslynSymbolStatus,
    bool OwnerMatches,
    string? MethodOrPropertySymbol,
    string? ContainingTypeSymbol,
    string? ReceiverTypeSymbol,
    IReadOnlyList<StringArgumentHit> StringArguments);

internal sealed record StringArgumentHit(
    int Index,
    string? Name,
    string ExpressionKind,
    string Expression,
    string? ConstantValue,
    bool HasQudMarkup,
    bool HasTmpMarkup,
    bool HasPlaceholderLikeText);

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ProbeResult))]
internal sealed partial class ProbeJsonContext : JsonSerializerContext;
