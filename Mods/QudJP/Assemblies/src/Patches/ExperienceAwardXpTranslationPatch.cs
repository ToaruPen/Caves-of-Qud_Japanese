using System;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;

namespace QudJP.Patches;

[HarmonyPatch]
public static class ExperienceAwardXpTranslationPatch
{
    private const string Context = nameof(ExperienceAwardXpTranslationPatch);

    [ThreadStatic]
    private static int activeDepth;

    [HarmonyTargetMethod]
    private static MethodBase? TargetMethod()
    {
        var experienceType = AccessTools.TypeByName("XRL.World.Parts.Experience");
        if (experienceType is null)
        {
            Trace.TraceError("QudJP: ExperienceAwardXpTranslationPatch target type not found.");
            return null;
        }

        var awardXpEventType = AccessTools.TypeByName("XRL.World.AwardXPEvent");
        if (awardXpEventType is null)
        {
            Trace.TraceError("QudJP: ExperienceAwardXpTranslationPatch AwardXPEvent type not found.");
            return null;
        }

        var method = AccessTools.Method(experienceType, "HandleEvent", new[] { awardXpEventType });
        if (method is null)
        {
            Trace.TraceError("QudJP: ExperienceAwardXpTranslationPatch.HandleEvent(AwardXPEvent) not found.");
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
            Trace.TraceError("QudJP: ExperienceAwardXpTranslationPatch.Prefix failed: {0}", ex);
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
            Trace.TraceError("QudJP: ExperienceAwardXpTranslationPatch.Finalizer failed: {0}", ex);
        }

        return __exception;
    }

    internal static bool TryTranslateQueuedMessage(ref string message, string? color)
    {
        _ = color;

        return activeDepth > 0
            && IsGuardedMessage(message)
            && MessageLogProducerTranslationHelpers.TryPreparePatternMessage(ref message, Context, "Experience.HandleEvent");
    }

    private static bool IsGuardedMessage(string message)
    {
        return !string.IsNullOrEmpty(message)
            && message.StartsWith("You gain ", StringComparison.Ordinal)
            && (message.EndsWith(" XP", StringComparison.Ordinal)
                || message.EndsWith(" XP.", StringComparison.Ordinal)
                || message.EndsWith(" XP!", StringComparison.Ordinal));
    }
}
