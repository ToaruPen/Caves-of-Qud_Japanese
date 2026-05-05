using System;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;

namespace QudJP.Patches;

[HarmonyPatch]
public static class PickTargetWindowUpdateTranslationPatch
{
    [HarmonyTargetMethod]
    private static MethodBase? TargetMethod()
    {
        var method = AccessTools.Method("Qud.UI.PickTargetWindow:Update", Type.EmptyTypes);
        if (method is null)
        {
            Trace.TraceError("QudJP: Failed to resolve Qud.UI.PickTargetWindow.Update. Patch will not apply.");
        }

        return method;
    }

    public static void Prefix()
    {
        try
        {
            TranslateCurrentText();
        }
        catch (Exception ex)
        {
            Trace.TraceError("QudJP: PickTargetWindowUpdateTranslationPatch.Prefix failed: {0}", ex);
        }
    }

    internal static bool TranslateCurrentTextForTests(Type pickTargetWindowType)
    {
        return TranslateCurrentText(pickTargetWindowType);
    }

    private static void TranslateCurrentText()
    {
        var type = AccessTools.TypeByName("Qud.UI.PickTargetWindow");
        _ = TranslateCurrentText(type);
    }

    private static bool TranslateCurrentText(Type? pickTargetWindowType)
    {
        var field = pickTargetWindowType is null
            ? null
            : AccessTools.Field(pickTargetWindowType, "currentText");
        if (field is null || field.FieldType != typeof(string))
        {
            return false;
        }

        var current = field.GetValue(null) as string;
        if (string.IsNullOrEmpty(current))
        {
            return false;
        }

        if (!PickTargetWindowTextTranslator.TryTranslateUiText(
                current!,
                nameof(PickTargetWindowUpdateTranslationPatch),
                out var translated)
            || string.Equals(current, translated, StringComparison.Ordinal))
        {
            return false;
        }

        field.SetValue(null, translated);
        return true;
    }
}
