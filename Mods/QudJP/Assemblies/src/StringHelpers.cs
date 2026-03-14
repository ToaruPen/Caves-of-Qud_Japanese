using System;

namespace QudJP;

internal static class StringHelpers
{
    internal static bool ContainsOrdinalIgnoreCase(string source, string value)
    {
#if NET48
        return source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
#else
        return source.Contains(value, StringComparison.OrdinalIgnoreCase);
#endif
    }

    internal static bool EqualsOrdinalIgnoreCase(string source, string value)
    {
        return string.Equals(source, value, StringComparison.OrdinalIgnoreCase);
    }
}
