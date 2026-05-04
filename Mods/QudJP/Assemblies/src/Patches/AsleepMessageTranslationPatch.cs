using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace QudJP.Patches;

[HarmonyPatch]
public static class AsleepMessageTranslationPatch
{
    private const string Context = nameof(AsleepMessageTranslationPatch);
    private const string FallAsleepText = "You fall {{C|asleep}}!";
    private const string AreAsleepText = "You are asleep.";

    private static readonly MethodInfo TranslateLiteralMethod =
        AccessTools.Method(typeof(AsleepMessageTranslationPatch), nameof(TranslateLiteral))
        ?? throw new InvalidOperationException("AsleepMessageTranslationPatch.TranslateLiteral method not found.");

    [HarmonyTargetMethods]
    private static IEnumerable<MethodBase> TargetMethods()
    {
        var targetType = AccessTools.TypeByName("XRL.World.Effects.Asleep");
        if (targetType is null)
        {
            Trace.TraceError("QudJP: {0} target type not found.", Context);
            yield break;
        }

        var applyMethod = AccessTools.Method(targetType, "Apply", new[] { AccessTools.TypeByName("XRL.World.GameObject") });
        if (applyMethod is null)
        {
            Trace.TraceError("QudJP: {0}.Apply(GameObject) not found.", Context);
        }
        else
        {
            yield return applyMethod;
        }

        var beginTakeActionEventType = AccessTools.TypeByName("XRL.World.BeginTakeActionEvent");
        var handleEventMethod = beginTakeActionEventType is null
            ? null
            : AccessTools.Method(targetType, "HandleEvent", new[] { beginTakeActionEventType });
        if (handleEventMethod is null)
        {
            Trace.TraceError("QudJP: {0}.HandleEvent(BeginTakeActionEvent) not found.", Context);
        }
        else
        {
            yield return handleEventMethod;
        }
    }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Ldstr
                && instruction.operand is string literal
                && IsTargetLiteral(literal))
            {
                yield return instruction;
                yield return new CodeInstruction(OpCodes.Call, TranslateLiteralMethod);
                continue;
            }

            yield return instruction;
        }
    }

    internal static string TranslateLiteral(string source)
    {
        var translated = Translator.Translate(source);
        if (!string.Equals(translated, source, StringComparison.Ordinal))
        {
            DynamicTextObservability.RecordTransform(Context, "Asleep.MessageLiteral", source, translated);
        }

        return translated;
    }

    private static bool IsTargetLiteral(string literal)
    {
        return string.Equals(literal, FallAsleepText, StringComparison.Ordinal)
            || string.Equals(literal, AreAsleepText, StringComparison.Ordinal);
    }
}
