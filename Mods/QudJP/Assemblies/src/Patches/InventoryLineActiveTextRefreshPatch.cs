using System;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;

namespace QudJP.Patches;

[HarmonyPatch]
public static class InventoryLineActiveTextRefreshPatch
{
    private const string TargetTypeName = "Qud.UI.InventoryLine";

    [HarmonyTargetMethod]
    private static MethodBase? TargetMethod()
    {
        var targetType = AccessTools.TypeByName(TargetTypeName);
        var method = targetType is null ? null : AccessTools.Method(targetType, "LateUpdate");
        if (method is null)
        {
            Trace.TraceError("QudJP: Failed to resolve InventoryLine.LateUpdate(). Active text refresh patch will not apply.");
        }

        return method;
    }

    public static void Postfix(object __instance)
    {
        try
        {
#if HAS_TMP
            if (InventoryLineFontFixer.IsActiveItemLine(__instance)
                && !InventoryLineFontFixer.HasActiveReplacementForCurrentItemText(__instance)
                && !InventoryLineFontFixer.TryRefreshActiveItemLine(__instance))
            {
                DelayedInventoryLineRepairScheduler.ScheduleRepairForCurrentText(
                    __instance,
                    InventoryLineFontFixer.GetActiveItemLineText(__instance));
            }
#else
            _ = __instance;
#endif
        }
        catch (Exception ex)
        {
            Trace.TraceError("QudJP: InventoryLineActiveTextRefreshPatch.Postfix failed: {0}", ex);
        }
    }
}
