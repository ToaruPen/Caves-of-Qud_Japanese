using System;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;

namespace QudJP.Patches;

[HarmonyPatch]
public static class PopupShowSpaceTranslationPatch
{
    private const string Context = nameof(PopupShowSpaceTranslationPatch);
    private const string TargetTypeName = "XRL.UI.Popup";

    [HarmonyTargetMethod]
    private static MethodBase? TargetMethod()
    {
        var targetType = AccessTools.TypeByName(TargetTypeName);
        if (targetType is null)
        {
            Trace.TraceError($"QudJP: {Context} target type '{TargetTypeName}' not found.");
            return null;
        }

        var renderableType = AccessTools.TypeByName("ConsoleLib.Console.Renderable");
        MethodInfo? method = null;
        if (renderableType is not null)
        {
            method = AccessTools.Method(
                targetType,
                "ShowSpace",
                new[]
                {
                    typeof(string),
                    typeof(string),
                    typeof(string),
                    renderableType,
                    typeof(bool),
                    typeof(bool),
                    typeof(string),
                });
        }

        method ??= AccessTools.Method(targetType, "ShowSpace");
        if (method is null)
        {
            Trace.TraceError($"QudJP: {Context} method 'ShowSpace' not found on '{TargetTypeName}'.");
        }

        return method;
    }

    public static void Prefix(object[] __args)
    {
        try
        {
            if (__args.Length == 0 || __args[0] is not string message || string.IsNullOrEmpty(message))
            {
                return;
            }

            __args[0] = PopupTranslationPatch.TranslatePopupTextForProducerRoute(message, Context);
        }
        catch (Exception ex)
        {
            Trace.TraceError("QudJP: {0}.Prefix failed: {1}", Context, ex);
        }
    }
}
