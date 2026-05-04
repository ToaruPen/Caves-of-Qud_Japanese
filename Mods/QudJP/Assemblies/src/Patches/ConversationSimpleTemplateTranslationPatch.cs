using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;

namespace QudJP.Patches;

[HarmonyPatch]
public static class ConversationSimpleTemplateTranslationPatch
{
    [HarmonyTargetMethods]
    private static IEnumerable<MethodBase> TargetMethods()
    {
        var target = AccessTools.Method(
            "Qud.API.ConversationsAPI:addSimpleConversationToObject",
            new[]
            {
                AccessTools.TypeByName("XRL.World.GameObject"),
                typeof(string),
                typeof(string),
                typeof(string),
                typeof(string),
                typeof(string),
                typeof(bool),
                typeof(bool),
            });

        if (target is null)
        {
            Trace.TraceError("QudJP: Failed to resolve ConversationsAPI.addSimpleConversationToObject simple overload.");
            yield break;
        }

        yield return target;
    }

    public static void Prefix(ref string Text, ref string Goodbye)
    {
        try
        {
            Text = ConversationTemplateTranslator.TranslateTemplate(Text);
            Goodbye = ConversationTemplateTranslator.TranslateTemplate(Goodbye);
        }
        catch (Exception ex)
        {
            Trace.TraceError("QudJP: ConversationSimpleTemplateTranslationPatch.Prefix failed: {0}", ex);
        }
    }
}
