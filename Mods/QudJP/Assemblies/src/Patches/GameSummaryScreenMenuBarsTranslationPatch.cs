using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace QudJP.Patches;

[HarmonyPatch]
public static class GameSummaryScreenMenuBarsTranslationPatch
{
    private const string Context = nameof(GameSummaryScreenMenuBarsTranslationPatch);
    private const string SaveTombstoneFileText = "Save Tombstone File";
    private const string ExitText = "Exit";

    private static readonly MethodInfo TranslateLiteralMethod =
        AccessTools.Method(typeof(GameSummaryScreenMenuBarsTranslationPatch), nameof(TranslateLiteral))
        ?? throw new InvalidOperationException("GameSummaryScreenMenuBarsTranslationPatch.TranslateLiteral method not found.");

    [HarmonyTargetMethod]
    private static MethodBase? TargetMethod()
    {
        var targetType = GameTypeResolver.FindType("Qud.UI.GameSummaryScreen", "GameSummaryScreen");
        if (targetType is null)
        {
            Trace.TraceError("QudJP: GameSummaryScreenMenuBarsTranslationPatch target type not found.");
            return null;
        }

        var method = AccessTools.Method(targetType, "UpdateMenuBars", Type.EmptyTypes);
        if (method is null)
        {
            Trace.TraceError("QudJP: GameSummaryScreenMenuBarsTranslationPatch.UpdateMenuBars() not found.");
        }

        return method;
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
            DynamicTextObservability.RecordTransform(Context, "GameSummaryScreen.MenuOption", source, translated);
        }

        return translated;
    }

    private static bool IsTargetLiteral(string literal)
    {
        return string.Equals(literal, SaveTombstoneFileText, StringComparison.Ordinal)
            || string.Equals(literal, ExitText, StringComparison.Ordinal);
    }
}
