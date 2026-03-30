using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace QudJP.Patches;

[HarmonyPatch]
public static class CharGenCustomizeTranslationPatch
{
    private const string Context = nameof(CharGenCustomizeTranslationPatch);
    private const string TargetTypeName = "XRL.CharacterBuilds.Qud.UI.QudCustomizeCharacterModuleWindow";

    private static readonly HashSet<string> TranslatableLiterals = new HashSet<string>(StringComparer.Ordinal)
    {
        "Name: ",
        "<random>",
        "Gender: ",
        "Pronoun Set: ",
        "Pet: ",
        "<none>",
        "Enter name:",
        "Choose Gender",
        "Select Base Gender",
        "Choose Pronoun Set",
        "Select Base Set",
        "Choose Pet",
        "<create new>",
        "<from gender>",
    };

    private static readonly MethodInfo TranslateLiteralMethod =
        AccessTools.Method(typeof(CharGenCustomizeTranslationPatch), nameof(TranslateLiteral))
        ?? throw new InvalidOperationException("TranslateLiteral method not found.");

    [HarmonyTargetMethods]
    private static IEnumerable<MethodBase> TargetMethods()
    {
        var targetType = AccessTools.TypeByName(TargetTypeName);
        if (targetType is null)
        {
            Trace.TraceWarning("QudJP: {0} target type '{1}' not found.", Context, TargetTypeName);
            yield break;
        }

        foreach (var methodName in new[]
                 {
                     "GetSelections",
                     "SelectMenuOption",
                     "OnChooseGenderAsync",
                     "OnChoosePronounSetAsync",
                     "OnChoosePet",
                 })
        {
            var sourceMethod = AccessTools.Method(targetType, methodName);
            if (sourceMethod is null)
            {
                Trace.TraceWarning("QudJP: {0} method '{1}' not found on '{2}'.", Context, methodName, targetType.FullName);
                continue;
            }

            var targetMethod = ResolveStateMachineMoveNext(sourceMethod) ?? sourceMethod;
            yield return targetMethod;
        }
    }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            yield return instruction;

            if (instruction.opcode == OpCodes.Ldstr
                && instruction.operand is string literal
                && TranslatableLiterals.Contains(literal))
            {
                yield return new CodeInstruction(OpCodes.Call, TranslateLiteralMethod);
            }
        }
    }

    public static string TranslateLiteral(string source)
    {
        return CharGenProducerTranslationHelpers.TranslateText(source);
    }

    private static MethodInfo? ResolveStateMachineMoveNext(MethodInfo sourceMethod)
    {
        var asyncStateMachine = sourceMethod.GetCustomAttribute<AsyncStateMachineAttribute>();
        if (asyncStateMachine?.StateMachineType is not null)
        {
            return AccessTools.Method(asyncStateMachine.StateMachineType, "MoveNext");
        }

        var iteratorStateMachine = sourceMethod.GetCustomAttribute<IteratorStateMachineAttribute>();
        if (iteratorStateMachine?.StateMachineType is not null)
        {
            return AccessTools.Method(iteratorStateMachine.StateMachineType, "MoveNext");
        }

        return null;
    }
}
