using System;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;

namespace QudJP.Patches;

[HarmonyPatch]
public static class MainMenuLocalizationPatch
{
    private const string TargetTypeName = "Qud.UI.MainMenu";

    [HarmonyTargetMethod]
    private static MethodBase? TargetMethod()
    {
        var method = AccessTools.Method(TargetTypeName + ":Show");
        if (method is null)
        {
            Trace.TraceError("QudJP: Failed to resolve Qud.UI.MainMenu.Show(). Patch will not apply.");
        }

        return method;
    }

    public static void Postfix(object __instance)
    {
        var targetType = __instance?.GetType() ?? AccessTools.TypeByName(TargetTypeName);
        if (targetType is null)
        {
            Trace.TraceError("QudJP: MainMenuLocalizationPatch target type is null. Skipping translation.");
            return;
        }

        var leftOptions = AccessCollectionField(targetType, __instance, "LeftOptions");
        UITextSkinTranslationPatch.TranslateStringFieldsInCollection(leftOptions, "Text");

        var rightOptions = AccessCollectionField(targetType, __instance, "RightOptions");
        UITextSkinTranslationPatch.TranslateStringFieldsInCollection(rightOptions, "Text");
    }

    private static object? AccessCollectionField(Type targetType, object? instance, string fieldName)
    {
        var field = AccessTools.Field(targetType, fieldName);
        if (field is null)
        {
            return null;
        }

        if (field.IsStatic)
        {
            return field.GetValue(null);
        }

        if (instance is null)
        {
            return null;
        }

        return field.GetValue(instance);
    }
}
