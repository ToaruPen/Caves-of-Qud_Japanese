using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace QudJP.Patches;

internal static class GeneratedQuestTitleTranslator
{
    private static readonly Regex FindSpecificItemPattern =
        new Regex(
            "^(?<helping>Helping|Assisting|Aiding) (?<giver>.+?) to Find (?<item>.+)$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex EmbeddedFindSpecificItemPattern =
        new Regex(
            "(?<title>(?<helping>Helping|Assisting|Aiding) (?<giver>.+?) to Find (?<item>[^\\r\\n]+))",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

    internal static string TranslatePreservingColors(string source, string route)
    {
        if (string.IsNullOrEmpty(source))
        {
            return source ?? string.Empty;
        }

        var (stripped, spans) = ColorAwareTranslationComposer.Strip(source);
        if (!TryTranslateFindSpecificItemTitle(stripped, spans, route, out var translated))
        {
            return source;
        }

        return spans.Count == 0
            ? translated
            : ColorAwareTranslationComposer.RestoreSourceBoundaryWrappersByVisibleTextPreservingTranslatedOwnership(
                translated,
                spans,
                stripped);
    }

    internal static string TranslateEmbeddedPreservingColors(string source, string route)
    {
        if (string.IsNullOrEmpty(source))
        {
            return source ?? string.Empty;
        }

        var (stripped, spans) = ColorAwareTranslationComposer.Strip(source);
        var match = EmbeddedFindSpecificItemPattern.Match(stripped);
        if (!match.Success
            || !TryTranslateFindSpecificItemTitle(match, spans, route, out var translatedTitle))
        {
            return source;
        }

        var titleGroup = match.Groups["title"];
        var translated = stripped.Substring(0, titleGroup.Index)
            + translatedTitle
            + stripped.Substring(titleGroup.Index + titleGroup.Length);
        return spans.Count == 0
            ? translated
            : ColorAwareTranslationComposer.RestoreSourceBoundaryWrappersByVisibleTextPreservingTranslatedOwnership(
                translated,
                spans,
                stripped);
    }

    internal static bool TryTranslatePreservingColors(string source, string route, out string translated)
    {
        translated = TranslatePreservingColors(source, route);
        return !string.Equals(translated, source, StringComparison.Ordinal);
    }

    private static bool TryTranslateFindSpecificItemTitle(
        string source,
        IReadOnlyList<ColorSpan> spans,
        string route,
        out string translated)
    {
        var match = FindSpecificItemPattern.Match(source);
        if (!match.Success
            || !TryTranslateFindSpecificItemTitle(match, spans, route, out translated))
        {
            translated = source;
            return false;
        }

        return true;
    }

    private static bool TryTranslateFindSpecificItemTitle(
        Match match,
        IReadOnlyList<ColorSpan> spans,
        string route,
        out string translated)
    {
        var giver = ColorAwareTranslationComposer.RestoreCapture(
            match.Groups["giver"].Value,
            spans,
            match.Groups["giver"]);
        var itemWithWrappers = ColorAwareTranslationComposer.RestoreCaptureWholeBoundaryWrappersPreservingTranslatedOwnership(
            match.Groups["item"].Value,
            spans,
            match.Groups["item"]);
        var item = StripLeadingEnglishArticlePreservingColors(itemWithWrappers);
        translated = giver + "が" + item + "を探すのを助ける";
        var sourceTitle = match.Groups["title"].Success ? match.Groups["title"].Value : match.Value;
        DynamicTextObservability.RecordTransform(route, "GeneratedQuestTitle.FindSpecificItem", sourceTitle, translated);
        return true;
    }

    private static string StripLeadingEnglishArticlePreservingColors(string source)
    {
        var visible = ColorAwareTranslationComposer.GetVisibleText(source);
        var withoutArticle = StringHelpers.StripLeadingEnglishArticle(visible);
        if (string.Equals(withoutArticle, visible, StringComparison.Ordinal))
        {
            return source;
        }

        return ColorAwareTranslationComposer.TranslatePreservingColors(source, _ => withoutArticle);
    }
}
