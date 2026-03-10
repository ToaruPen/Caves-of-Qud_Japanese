using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;

namespace QudJP.Patches;

[HarmonyPatch]
public static class UITextSkinTranslationPatch
{
    [HarmonyTargetMethod]
    private static MethodBase? TargetMethod()
    {
        return AccessTools.Method("XRL.UI.UITextSkin:SetText", new[] { typeof(string) });
    }

    public static void Prefix(ref string text)
    {
        text = TranslatePreservingColors(text);
    }

    internal static string TranslatePreservingColors(string? source)
    {
        if (string.IsNullOrEmpty(source))
        {
            return source ?? string.Empty;
        }

        var (stripped, spans) = ColorCodePreserver.Strip(source);
        if (stripped.Length == 0)
        {
            return source!;
        }

        var translated = Translator.Translate(stripped);
        return ColorCodePreserver.Restore(translated, spans);
    }

    internal static void TranslateStringField(object? instance, string fieldName)
    {
        if (instance is null || string.IsNullOrEmpty(fieldName))
        {
            return;
        }

        var field = AccessTools.Field(instance.GetType(), fieldName);
        if (field is null || field.FieldType != typeof(string))
        {
            return;
        }

        var current = field.GetValue(instance) as string;
        var translated = TranslatePreservingColors(current);
        if (!string.Equals(current, translated, StringComparison.Ordinal))
        {
            field.SetValue(instance, translated);
        }
    }

    internal static void TranslateStringFieldsInCollection(object? maybeCollection, params string[] fieldNames)
    {
        if (maybeCollection is null || maybeCollection is string || fieldNames is null || fieldNames.Length == 0)
        {
            return;
        }

        if (maybeCollection is not IEnumerable enumerable)
        {
            return;
        }

        foreach (var item in enumerable)
        {
            if (item is null)
            {
                continue;
            }

            for (var index = 0; index < fieldNames.Length; index++)
            {
                TranslateStringField(item, fieldNames[index]);
            }
        }
    }
}
