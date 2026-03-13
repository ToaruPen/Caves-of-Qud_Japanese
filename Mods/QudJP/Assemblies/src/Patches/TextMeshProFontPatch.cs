#if HAS_TMP
using System;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;
using TMPro;

namespace QudJP.Patches;

[HarmonyPatch]
public static class TextMeshProFontPatch
{
    [HarmonyTargetMethod]
    private static MethodBase? TargetMethod()
    {
        var method = AccessTools.Method(typeof(TextMeshPro), "OnEnable");
        if (method is null)
        {
            Trace.TraceError("QudJP: Failed to resolve TextMeshPro.OnEnable(). Patch will not apply.");
        }

        return method;
    }

    public static void Postfix(TextMeshPro __instance)
    {
        try
        {
            FontManager.ApplyToText(__instance);
        }
        catch (Exception ex)
        {
            Trace.TraceError("QudJP: TextMeshProFontPatch.Postfix failed: {0}", ex);
        }
    }
}
#endif
