using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using HarmonyLib;

namespace QudJP.Patches;

[HarmonyPatch]
public static class ConversationDisplayTextPatch
{
    private static readonly Regex TrailingActionMarkerPattern =
        new Regex(" (?<marker>\\[[^\\]]+\\])$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    [HarmonyTargetMethods]
    private static IEnumerable<MethodBase> TargetMethods()
    {
        var targets = new List<MethodBase>();

        AddTargetMethod(targets, "XRL.World.Conversations.IConversationElement");
        AddTargetMethod(targets, "XRL.World.Conversations.Choice");

        if (targets.Count == 0)
        {
            Trace.TraceError("QudJP: Failed to resolve conversation GetDisplayText(bool) targets. Patch will not apply.");
        }

        return targets;
    }

    private static void AddTargetMethod(List<MethodBase> targets, string typeName)
    {
        var method = AccessTools.Method(typeName + ":GetDisplayText", new[] { typeof(bool) });
        if (method is null)
        {
            Trace.TraceError("QudJP: Failed to resolve {0}.GetDisplayText(bool).", typeName);
            return;
        }

        targets.Add(method);
    }

    public static void Postfix(ref string __result)
    {
        try
        {
            if (string.IsNullOrEmpty(__result))
            {
                return;
            }

            __result = NormalizeConversationDisplayText(__result);
            __result = TranslateConversationDisplayText(__result);
        }
        catch (Exception ex)
        {
            Trace.TraceError("QudJP: ConversationDisplayTextPatch.Postfix failed: {0}", ex);
        }
    }

    private static string NormalizeConversationDisplayText(string source)
    {
        var match = TrailingActionMarkerPattern.Match(source);
        if (!match.Success)
        {
            return source;
        }

        return source.Substring(0, match.Index);
    }

    private static string TranslateConversationDisplayText(string source)
    {
        var route = nameof(ConversationDisplayTextPatch);
        return ColorAwareTranslationComposer.TranslatePreservingColors(
            source,
            visible => TryTranslateVisibleConversationText(visible, route, out var translated)
                ? translated
                : visible);
    }

    private static bool TryTranslateVisibleConversationText(string source, string route, out string translated)
    {
        if (StringHelpers.TryGetTranslationExactOrLowerAscii(source, out translated)
            && !string.Equals(source, translated, StringComparison.Ordinal))
        {
            DynamicTextObservability.RecordTransform(route, "ConversationDisplay.ExactLeaf", source, translated);
            return true;
        }

        translated = source;
        return false;
    }
}
