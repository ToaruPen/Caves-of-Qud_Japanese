using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;

namespace QudJP.Patches;

[HarmonyPatch]
public static class CharGenPregenSelectionTranslationPatch
{
    private const string Context = nameof(CharGenPregenSelectionTranslationPatch);
    private const string TargetTypeName = "XRL.CharacterBuilds.Qud.UI.QudPregenModuleWindow";

    [HarmonyTargetMethods]
    private static IEnumerable<MethodBase> TargetMethods()
    {
        var type = AccessTools.TypeByName(TargetTypeName);
        if (type is null)
        {
            Trace.TraceWarning("QudJP: {0} target type '{1}' not found.", Context, TargetTypeName);
            yield break;
        }

        var method = AccessTools.Method(type, "GetSelections", Type.EmptyTypes);
        if (method is null)
        {
            Trace.TraceWarning("QudJP: {0} method 'GetSelections()' not found on '{1}'.", Context, type.FullName);
            yield break;
        }

        yield return method;
    }

    public static IEnumerable? Postfix(IEnumerable? values)
    {
        try
        {
            if (values is null)
            {
                return values;
            }

            var translatedTitles = CharGenProducerTranslationHelpers.MaterializeTranslatedEnumerable(
                values,
                "Title",
                Context);
            return CharGenProducerTranslationHelpers.MaterializeTranslatedEnumerable(
                translatedTitles,
                "Description",
                Context,
                CharGenProducerTranslationHelpers.TranslateStructuredText);
        }
        catch (Exception ex)
        {
            Trace.TraceError("QudJP: {0}.Postfix failed: {1}", Context, ex);
            return values;
        }
    }
}
