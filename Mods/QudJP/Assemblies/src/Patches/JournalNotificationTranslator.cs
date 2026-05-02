using System;

namespace QudJP.Patches;

internal static class JournalNotificationTranslator
{
    private const string PieceOfInformationPrefix = "You note this piece of information in the ";
    private const string NoteLocationPrefix = "You note the location of ";
    private const string DiscoverLocationPrefix = "You discover the location of ";
    private const string DiscoveredLocationPrefix = "You discovered the location of ";
    private const string JournalSectionSuffix = " section of your journal.";

    internal static bool TryTranslate(string source, string route, string family, out string translated)
    {
        var (stripped, _) = ColorAwareTranslationComposer.Strip(source);
        if (!IsJournalNotification(stripped))
        {
            translated = source;
            return false;
        }

        var journalTranslated = JournalPatternTranslator.Translate(stripped, route);
        if (string.Equals(journalTranslated, stripped, StringComparison.Ordinal))
        {
            translated = source;
            return false;
        }

        translated = journalTranslated;
        DynamicTextObservability.RecordTransform(route, family, source, translated);
        return true;
    }

    private static bool IsJournalNotification(string source)
    {
        if (source.StartsWith(PieceOfInformationPrefix, StringComparison.Ordinal)
            && source.EndsWith(JournalSectionSuffix, StringComparison.Ordinal))
        {
            return true;
        }

        if (source.StartsWith(NoteLocationPrefix, StringComparison.Ordinal)
            && source.EndsWith(JournalSectionSuffix, StringComparison.Ordinal))
        {
            return true;
        }

        return source.StartsWith(DiscoverLocationPrefix, StringComparison.Ordinal)
            || source.StartsWith(DiscoveredLocationPrefix, StringComparison.Ordinal);
    }
}
