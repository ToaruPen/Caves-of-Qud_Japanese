using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;

namespace QudJP.Patches;

[HarmonyPatch]
public static class HistoricStringExpanderPatch
{
    private const string Context = nameof(HistoricStringExpanderPatch);

    [HarmonyTargetMethods]
    private static IEnumerable<MethodBase> TargetMethods()
    {
        Trace.TraceWarning("QudJP: HistoricStringExpanderPatch is temporarily disabled to avoid corrupting HistorySpice/world generation output.");
        yield break;
    }

    public static void Postfix(ref string __result)
    {
        try
        {
            var source = __result;
            var translated = TranslateExpandedText(source);
            if (!string.Equals(translated, source, StringComparison.Ordinal))
            {
                DynamicTextObservability.RecordTransform(
                    Context,
                    "HistoricStringExpander.ExactLeaf",
                    source ?? string.Empty,
                    translated);
            }

            __result = translated;
        }
        catch (Exception ex)
        {
            Trace.TraceError("QudJP: HistoricStringExpanderPatch.Postfix failed: {0}", ex);
        }
    }

    internal static string TranslateExpandedText(string? source)
    {
        if (string.IsNullOrEmpty(source))
        {
            return source ?? string.Empty;
        }

        if (MessageFrameTranslator.TryStripDirectTranslationMarker(source, out var markedText))
        {
            return markedText;
        }

        return ColorAwareTranslationComposer.TranslatePreservingColors(
            source,
            static visible => StringHelpers.TryGetTranslationExactOrLowerAscii(visible, out var translated)
                ? translated
                : visible);
    }
}
