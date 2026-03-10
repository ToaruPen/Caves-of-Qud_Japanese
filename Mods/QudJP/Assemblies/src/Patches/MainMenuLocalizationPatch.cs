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
        return AccessTools.Method(TargetTypeName + ":Show");
    }

    public static void Postfix(object __instance)
    {
        var targetType = __instance?.GetType() ?? AccessTools.TypeByName(TargetTypeName);
        if (targetType is null)
        {
            return;
        }

        var leftOptions = AccessTools.Field(targetType, "LeftOptions")?.GetValue(null);
        UITextSkinTranslationPatch.TranslateStringFieldsInCollection(leftOptions, "Text");

        var rightOptions = AccessTools.Field(targetType, "RightOptions")?.GetValue(null);
        UITextSkinTranslationPatch.TranslateStringFieldsInCollection(rightOptions, "Text");
    }
}
