using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;

namespace QudJP.Patches;

[HarmonyPatch]
public static class TinkeringStatusScreenTranslationPatch
{
    private const string Context = nameof(TinkeringStatusScreenTranslationPatch);

    [HarmonyTargetMethod]
    private static MethodBase? TargetMethod()
    {
        var targetType = GameTypeResolver.FindType("Qud.UI.TinkeringStatusScreen", "TinkeringStatusScreen");
        if (targetType is null)
        {
            Trace.TraceError("QudJP: TinkeringStatusScreenTranslationPatch target type not found.");
            return null;
        }

        var method = AccessTools.Method(targetType, "UpdateViewFromData", Type.EmptyTypes);
        if (method is null)
        {
            Trace.TraceError("QudJP: TinkeringStatusScreenTranslationPatch.UpdateViewFromData() not found.");
        }

        return method;
    }

    public static void Postfix(object? __instance)
    {
        try
        {
            if (__instance is null)
            {
                return;
            }

            TranslateModeToggle(__instance);
            TranslateCategoryInfos(__instance);
        }
        catch (Exception ex)
        {
            Trace.TraceError("QudJP: TinkeringStatusScreenTranslationPatch.Postfix failed: {0}", ex);
        }
    }

    private static void TranslateModeToggle(object instance)
    {
        var modeToggleText = UiBindingTranslationHelpers.GetMemberValue(instance, "modeToggleText");
        var current = UITextSkinReflectionAccessor.GetCurrentText(modeToggleText, Context);
        if (string.IsNullOrEmpty(current)
            || !StringHelpers.TryGetTranslationExactOrLowerAscii(current!, out var translated)
            || string.Equals(translated, current, StringComparison.Ordinal))
        {
            return;
        }

        var route = ObservabilityHelpers.ComposeContext(Context, "field=modeToggleText");
        DynamicTextObservability.RecordTransform(route, "TinkeringStatus.ModeToggleText", current!, translated);
        OwnerTextSetter.SetTranslatedText(
            modeToggleText,
            current!,
            translated,
            Context,
            typeof(TinkeringStatusScreenTranslationPatch));
    }

    private static void TranslateCategoryInfos(object instance)
    {
        if (UiBindingTranslationHelpers.GetMemberValue(instance, "categoryInfos") is not IEnumerable categoryInfos)
        {
            return;
        }

        var index = 0;
        foreach (var categoryInfo in categoryInfos)
        {
            if (categoryInfo is null)
            {
                index++;
                continue;
            }

            var current = UiBindingTranslationHelpers.GetStringMemberValue(categoryInfo, "Name");
            if (string.IsNullOrEmpty(current)
                || !StringHelpers.TryGetTranslationExactOrLowerAscii(current!, out var translated)
                || string.Equals(translated, current, StringComparison.Ordinal))
            {
                index++;
                continue;
            }

            UiBindingTranslationHelpers.SetMemberValue(categoryInfo, "Name", translated);
            var route = ObservabilityHelpers.ComposeContext(Context, "field=categoryInfos[" + index + "].Name");
            DynamicTextObservability.RecordTransform(route, "TinkeringStatus.CategoryName", current!, translated);
            index++;
        }
    }
}
