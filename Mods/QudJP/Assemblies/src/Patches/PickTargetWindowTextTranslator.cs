using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace QudJP.Patches;

internal static class PickTargetWindowTextTranslator
{
    private const string DictionaryFile = "ui-pick-target.ja.json";

    internal static bool TryTranslateUiText(string source, string route, out string translated)
    {
        if (TryTranslateCommandBar(source, route, out translated))
        {
            return true;
        }

        if (StringHelpers.TryGetTranslationExactOrLowerAscii(source, out translated))
        {
            DynamicTextObservability.RecordTransform(route, "PickTarget.ExactLookup", source, translated);
            return true;
        }

        translated = source;
        return false;
    }

    private static bool TryTranslateCommandBar(string source, string route, out string translated)
    {
        translated = source;
        if (source.IndexOf(" | ", StringComparison.Ordinal) < 0)
        {
            return false;
        }

        var segments = source.Split(new[] { " | " }, StringSplitOptions.None);
        var translatedSegments = new string[segments.Length];
        for (var index = 0; index < segments.Length; index++)
        {
            if (!TryTranslateCommandBarSegment(segments[index], out translatedSegments[index]))
            {
                return false;
            }
        }

        translated = string.Join(" | ", translatedSegments);
        DynamicTextObservability.RecordTransform(route, "PickTarget.CommandBar", source, translated);
        return true;
    }

    private static bool TryTranslateCommandBarSegment(string source, out string translated)
    {
        var visible = ColorAwareTranslationComposer.GetVisibleText(source);
        if (IsOwnerRouteCommandBarToken(visible))
        {
            translated = source;
            return true;
        }

        var direct = TranslatePickTargetToken(visible, traceFallback: false);
        if (direct is not null)
        {
            translated = ColorAwareTranslationComposer.TranslatePreservingColors(source, _ => direct);
            return true;
        }

        if (UITextSkinTranslationPatch.LooksLikeCommandHotkeyToken(visible))
        {
            translated = source;
            return true;
        }

        var parenthesizedHotkeyMatch = Regex.Match(source, "^\\((?<hotkey>[^)]+)\\)\\s+(?<label>.+)$", RegexOptions.CultureInvariant);
        if (parenthesizedHotkeyMatch.Success)
        {
            var label = parenthesizedHotkeyMatch.Groups["label"].Value;
            var visibleLabel = ColorAwareTranslationComposer.GetVisibleText(label);
            if (IsOwnerRouteCommandBarToken(visibleLabel))
            {
                translated = source;
                return true;
            }

            var translatedLabel = TranslatePickTargetToken(visibleLabel, traceFallback: false);
            if (translatedLabel is not null)
            {
                translated = $"({parenthesizedHotkeyMatch.Groups["hotkey"].Value}) {ColorAwareTranslationComposer.TranslatePreservingColors(label, _ => translatedLabel)}";
                return true;
            }
        }

        var sourceParenthesizedHotkeyMatch = Regex.Match(source, "^(?<label>.+?)\\s+\\((?<hotkey>.+)\\)(?<suffix>\\)?)$", RegexOptions.CultureInvariant);
        if (sourceParenthesizedHotkeyMatch.Success)
        {
            var label = sourceParenthesizedHotkeyMatch.Groups["label"].Value;
            var visibleLabel = ColorAwareTranslationComposer.GetVisibleText(label);
            if (IsOwnerRouteCommandBarToken(visibleLabel))
            {
                translated = source;
                return true;
            }

            var translatedLabel = TranslatePickTargetToken(visibleLabel, traceFallback: false);
            if (translatedLabel is not null)
            {
                translated = $"{ColorAwareTranslationComposer.TranslatePreservingColors(label, _ => translatedLabel)} ({sourceParenthesizedHotkeyMatch.Groups["hotkey"].Value}){sourceParenthesizedHotkeyMatch.Groups["suffix"].Value}";
                return true;
            }
        }

        var hotkeyPrefixMatch = Regex.Match(source, "^(?<hotkey>\\S+)\\s+(?<label>.+)$", RegexOptions.CultureInvariant);
        if (hotkeyPrefixMatch.Success)
        {
            var label = hotkeyPrefixMatch.Groups["label"].Value;
            var visibleLabel = ColorAwareTranslationComposer.GetVisibleText(label);
            if (IsOwnerRouteCommandBarToken(visibleLabel))
            {
                translated = source;
                return true;
            }

            var translatedLabel = TranslatePickTargetToken(visibleLabel, traceFallback: false);
            if (translatedLabel is not null)
            {
                translated = $"{hotkeyPrefixMatch.Groups["hotkey"].Value} {ColorAwareTranslationComposer.TranslatePreservingColors(label, _ => translatedLabel)}";
                return true;
            }
        }

        var hyphenatedHotkeyMatch = Regex.Match(source, "^(?<hotkey>.+)-(?<label>[^\\s|-]+)$", RegexOptions.CultureInvariant);
        if (hyphenatedHotkeyMatch.Success)
        {
            var label = hyphenatedHotkeyMatch.Groups["label"].Value;
            var visibleLabel = ColorAwareTranslationComposer.GetVisibleText(label);
            if (IsOwnerRouteCommandBarToken(visibleLabel))
            {
                translated = source;
                return true;
            }

            var translatedLabel = TranslatePickTargetToken(visibleLabel, traceFallback: false);
            if (translatedLabel is not null)
            {
                translated = $"{hyphenatedHotkeyMatch.Groups["hotkey"].Value}-{ColorAwareTranslationComposer.TranslatePreservingColors(label, _ => translatedLabel)}";
                return true;
            }
        }

        var hotkeySuffixMatch = Regex.Match(source, "^(?<label>.+?)\\s+\\((?<hotkey>[^)]+)\\)(?<suffix>\\)?)$", RegexOptions.CultureInvariant);
        if (hotkeySuffixMatch.Success)
        {
            var label = hotkeySuffixMatch.Groups["label"].Value;
            var visibleLabel = ColorAwareTranslationComposer.GetVisibleText(label);
            if (IsOwnerRouteCommandBarToken(visibleLabel))
            {
                translated = source;
                return true;
            }

            var translatedLabel = TranslatePickTargetToken(visibleLabel, traceFallback: false);
            if (translatedLabel is not null)
            {
                translated = $"{ColorAwareTranslationComposer.TranslatePreservingColors(label, _ => translatedLabel)} ({hotkeySuffixMatch.Groups["hotkey"].Value}){hotkeySuffixMatch.Groups["suffix"].Value}";
                return true;
            }
        }

        translated = source;
        return false;
    }

    private static bool IsOwnerRouteCommandBarToken(string source)
    {
        return string.Equals(source, "Fire Missile Weapon", StringComparison.OrdinalIgnoreCase)
            || string.Equals(source, "Reload", StringComparison.OrdinalIgnoreCase);
    }

    private static string? TranslatePickTargetToken(string source, bool traceFallback = true)
    {
        var scoped = ScopedDictionaryLookup.TranslateExactOrLowerAscii(source, DictionaryFile);
        if (scoped is not null)
        {
            return scoped;
        }

        if (traceFallback)
        {
            Trace.TraceWarning(
                "QudJP: {0} missing scoped UI token '{1}' in {2}; falling back to UITextSkin token lookup.",
                nameof(PickTargetWindowTextTranslator),
                source,
                DictionaryFile);
        }

        return UITextSkinTranslationPatch.TranslateAsciiTokenWithCaseFallback(source);
    }
}
