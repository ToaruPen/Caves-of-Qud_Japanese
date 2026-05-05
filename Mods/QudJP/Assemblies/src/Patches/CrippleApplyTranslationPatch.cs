using System;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;

namespace QudJP.Patches;

[HarmonyPatch]
public static class CrippleApplyTranslationPatch
{
    private const string Context = nameof(CrippleApplyTranslationPatch);

    [ThreadStatic]
    private static int activeDepth;

    [HarmonyTargetMethod]
    private static MethodBase? TargetMethod()
    {
        var crippleType = AccessTools.TypeByName("XRL.World.Effects.Cripple");
        if (crippleType is null)
        {
            Trace.TraceError("QudJP: CrippleApplyTranslationPatch target type not found.");
            return null;
        }

        var gameObjectType = AccessTools.TypeByName("XRL.World.GameObject");
        if (gameObjectType is null)
        {
            Trace.TraceError("QudJP: CrippleApplyTranslationPatch GameObject type not found.");
            return null;
        }

        var method = AccessTools.Method(crippleType, "Apply", new[] { gameObjectType });
        if (method is null)
        {
            Trace.TraceError("QudJP: CrippleApplyTranslationPatch.Apply(GameObject) not found.");
        }

        return method;
    }

    public static void Prefix()
    {
        try
        {
            activeDepth++;
        }
        catch (Exception ex)
        {
            Trace.TraceError("QudJP: CrippleApplyTranslationPatch.Prefix failed: {0}", ex);
        }
    }

    public static Exception? Finalizer(Exception? __exception)
    {
        try
        {
            if (activeDepth > 0)
            {
                activeDepth--;
            }
        }
        catch (Exception ex)
        {
            Trace.TraceError("QudJP: CrippleApplyTranslationPatch.Finalizer failed: {0}", ex);
        }

        return __exception;
    }

    internal static bool TryTranslateQueuedMessage(ref string message, string? color)
    {
        _ = color;

        return activeDepth > 0
            && IsGuardedMessage(message)
            && MessageLogProducerTranslationHelpers.TryPreparePatternMessage(ref message, Context, "Cripple.Apply");
    }

    private static bool IsGuardedMessage(string message)
    {
        return !string.IsNullOrEmpty(message)
            && message.StartsWith("You are crippled for ", StringComparison.Ordinal);
    }
}
