using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;

namespace QudJP.Patches;

[HarmonyPatch]
public static class ConversationQuestionTemplateTranslationPatch
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
                typeof(string),
                typeof(string),
                typeof(bool),
                typeof(bool),
            });

        if (target is null)
        {
            Trace.TraceError("QudJP: Failed to resolve ConversationsAPI.addSimpleConversationToObject question overload.");
            yield break;
        }

        yield return target;
    }

    public static void Prefix(ref string Text, ref string Goodbye, ref string Question, ref string Answer)
    {
        try
        {
            var translatedText = ConversationTemplateTranslator.TranslateTemplate(Text);
            var translatedGoodbye = ConversationTemplateTranslator.TranslateTemplate(Goodbye);
            var translatedQuestion = ConversationTemplateTranslator.TranslateTemplate(Question);
            var translatedAnswer = ConversationTemplateTranslator.TranslateTemplate(Answer);

            Text = translatedText;
            Goodbye = translatedGoodbye;
            Question = translatedQuestion;
            Answer = translatedAnswer;
        }
        catch (Exception ex)
        {
            Trace.TraceError("QudJP: ConversationQuestionTemplateTranslationPatch.Prefix failed: {0}", ex);
        }
    }
}
