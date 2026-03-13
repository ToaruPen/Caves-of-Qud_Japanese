#if HAS_TMP
using System;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;
using UnityEngine.UI;

namespace QudJP.Patches;

[HarmonyPatch]
public static class LegacyUITextFontPatch
{
    [HarmonyTargetMethod]
    private static MethodBase? TargetMethod()
    {
        var method = AccessTools.Method(typeof(Text), "OnEnable");
        if (method is null)
        {
            Trace.TraceError("QudJP: Failed to resolve UnityEngine.UI.Text.OnEnable(). Patch will not apply.");
        }

        return method;
    }

    public static void Postfix(Text __instance)
    {
        try
        {
            FontManager.ApplyToLegacyText(__instance);
        }
        catch (Exception ex)
        {
            Trace.TraceError("QudJP: LegacyUITextFontPatch.Postfix failed: {0}", ex);
        }
    }
}
#endif
