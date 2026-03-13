#if HAS_TMP
using System;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;
using TMPro;

namespace QudJP.Patches;

[HarmonyPatch]
public static class TmpInputFieldFontPatch
{
    [HarmonyTargetMethod]
    private static MethodBase? TargetMethod()
    {
        var method = AccessTools.Method(typeof(TMP_InputField), "OnEnable");
        if (method is null)
        {
            Trace.TraceError("QudJP: Failed to resolve TMP_InputField.OnEnable(). Patch will not apply.");
        }

        return method;
    }

    public static void Postfix(TMP_InputField __instance)
    {
        try
        {
            FontManager.ApplyToInputField(__instance);
        }
        catch (Exception ex)
        {
            Trace.TraceError("QudJP: TmpInputFieldFontPatch.Postfix failed: {0}", ex);
        }
    }
}
#endif
