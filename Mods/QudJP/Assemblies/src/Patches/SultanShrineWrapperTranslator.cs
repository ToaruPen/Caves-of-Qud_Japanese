using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace QudJP.Patches;

internal static class SultanShrineWrapperTranslator
{
    private const string TemplateKey = "QudJP.ShrineWrapper.AncientSultan";
    private const string FamilyTag = "ShrineWrapper.AncientSultan";

    private static readonly Regex CompositePattern =
        new Regex(
            "^The shrine depicts a significant event from the life of the ancient sultan (?<sultan>.+?):\\n\\n(?<gospel>.+?)\\n\\n(?<quality>.+?)$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);

    // Quality strings come from XRL.Rules.Strings.WoundLevel. The set is finite and shipped in
    // the world-shrine.ja.json dictionary; entries missing from the dictionary fall through to
    // the original English so a future game-side addition does not silently break the wrapper.
    private static readonly Dictionary<string, string> QualityKeys = new(StringComparer.Ordinal)
    {
        ["Perfect"] = "QudJP.ShrineWrapper.Quality.Perfect",
        ["Fine"] = "QudJP.ShrineWrapper.Quality.Fine",
        ["Lightly Damaged"] = "QudJP.ShrineWrapper.Quality.LightlyDamaged",
        ["Damaged"] = "QudJP.ShrineWrapper.Quality.Damaged",
        ["Badly Damaged"] = "QudJP.ShrineWrapper.Quality.BadlyDamaged",
        ["Undamaged"] = "QudJP.ShrineWrapper.Quality.Undamaged",
        ["Badly Wounded"] = "QudJP.ShrineWrapper.Quality.BadlyWounded",
        ["Wounded"] = "QudJP.ShrineWrapper.Quality.Wounded",
        ["Injured"] = "QudJP.ShrineWrapper.Quality.Injured",
    };

    internal static bool TryTranslateMessage(
        string source,
        IReadOnlyList<ColorSpan>? spans,
        string route,
        out string translated)
    {
        var match = CompositePattern.Match(source);
        if (!match.Success)
        {
            translated = source;
            return false;
        }

        if (!Translator.TryGetTranslation(TemplateKey, out var template))
        {
            translated = source;
            return false;
        }

        var sultanRaw = match.Groups["sultan"].Value;
        var gospelRaw = match.Groups["gospel"].Value;
        var qualityRaw = match.Groups["quality"].Value;

        var sultan = TranslateSultanName(sultanRaw);
        var gospel = TranslateGospel(gospelRaw, route);
        var quality = TranslateQuality(qualityRaw);

        if (!TryComposeWithKnownQualityOffset(template, sultan, gospel, quality, out var composed, out var qualityStart))
        {
            translated = source;
            return false;
        }

        if (spans is not null && spans.Count > 0 && qualityStart >= 0)
        {
            composed = WrapTranslatedQualityWithSourceColors(composed, qualityStart, quality.Length, spans, match.Groups["quality"]);
        }

        DynamicTextObservability.RecordTransform(
            nameof(MessagePatternTranslator),
            FamilyTag,
            source,
            composed);

        translated = composed;
        return true;
    }

    private static string TranslateSultanName(string source)
    {
        if (string.IsNullOrEmpty(source))
        {
            return source;
        }

        using var _ = Translator.PushMissingKeyLoggingSuppression(true);
        var direct = Translator.Translate(source);
        return string.Equals(direct, source, StringComparison.Ordinal) ? source : direct;
    }

    private static string TranslateGospel(string source, string route)
    {
        if (string.IsNullOrEmpty(source))
        {
            return source;
        }

        return JournalPatternTranslator.Translate(source, route);
    }

    private static string TranslateQuality(string source)
    {
        if (string.IsNullOrEmpty(source))
        {
            return source;
        }

        if (!QualityKeys.TryGetValue(source, out var key))
        {
            return source;
        }

        if (!Translator.TryGetTranslation(key, out var translation))
        {
            return source;
        }

        return translation;
    }

    // Computes the substituted output AND the absolute index where {quality} ends up after
    // {sultan} and {gospel} have been replaced. Defensive: requires the template to contain
    // each placeholder exactly once and {quality} to follow {sultan}/{gospel} in template order.
    private static bool TryComposeWithKnownQualityOffset(
        string template,
        string sultan,
        string gospel,
        string quality,
        out string composed,
        out int qualityStart)
    {
        const string sultanPlaceholder = "{sultan}";
        const string gospelPlaceholder = "{gospel}";
        const string qualityPlaceholder = "{quality}";

        var sultanIndex = template.IndexOf(sultanPlaceholder, StringComparison.Ordinal);
        var gospelIndex = template.IndexOf(gospelPlaceholder, StringComparison.Ordinal);
        var qualityIndex = template.IndexOf(qualityPlaceholder, StringComparison.Ordinal);
        if (sultanIndex < 0 || gospelIndex < 0 || qualityIndex < 0
            || sultanIndex >= gospelIndex || gospelIndex >= qualityIndex)
        {
            composed = string.Empty;
            qualityStart = -1;
            return false;
        }

        composed = template
            .Replace(sultanPlaceholder, sultan)
            .Replace(gospelPlaceholder, gospel)
            .Replace(qualityPlaceholder, quality);

        var sultanDelta = sultan.Length - sultanPlaceholder.Length;
        var gospelDelta = gospel.Length - gospelPlaceholder.Length;
        qualityStart = qualityIndex + sultanDelta + gospelDelta;
        return true;
    }

    private static string WrapTranslatedQualityWithSourceColors(
        string composed,
        int qualityStart,
        int qualityLength,
        IReadOnlyList<ColorSpan> spans,
        Group qualityGroup)
    {
        var qualityCaptureSpans = ColorCodePreserver.SliceSpans(spans, qualityGroup.Index, qualityGroup.Length);
        qualityCaptureSpans.AddRange(ColorCodePreserver.SliceAdjacentCaptureBoundarySpans(spans, qualityGroup.Index, qualityGroup.Length));
        if (qualityCaptureSpans.Count == 0)
        {
            return composed;
        }

        var prefix = composed.Substring(0, qualityStart);
        var middle = composed.Substring(qualityStart, qualityLength);
        var suffix = composed.Substring(qualityStart + qualityLength);
        var restoredMiddle = ColorAwareTranslationComposer.Restore(middle, qualityCaptureSpans);
        return prefix + restoredMiddle + suffix;
    }
}
