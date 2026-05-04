using System;

namespace QudJP.Patches;

public static class ConversationTemplateTranslator
{
    private const string Route = nameof(ConversationTemplateTranslator);

    public static string TranslateTemplate(string source)
    {
        if (string.IsNullOrEmpty(source))
        {
            return source;
        }

        return ColorAwareTranslationComposer.TranslatePreservingColors(
            source,
            visible => TryTranslateVisibleTemplate(visible, out var translated)
                ? translated
                : visible);
    }

    private static bool TryTranslateVisibleTemplate(string source, out string translated)
    {
        if (StringHelpers.TryGetTranslationExactOrLowerAscii(source, out translated)
            && !string.Equals(source, translated, StringComparison.Ordinal))
        {
            DynamicTextObservability.RecordTransform(Route, "ConversationTemplate.Exact", source, translated);
            return true;
        }

        translated = source;
        return false;
    }
}
