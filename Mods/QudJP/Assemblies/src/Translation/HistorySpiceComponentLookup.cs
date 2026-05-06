namespace QudJP;

internal static class HistorySpiceComponentLookup
{
    private const string DictionaryFile = "Scoped/historyspice-common.ja.json";

    internal static string? TranslateExactOrLowerAscii(string source) =>
        ScopedDictionaryLookup.TranslateExactOrLowerAscii(source, DictionaryFile);
}
