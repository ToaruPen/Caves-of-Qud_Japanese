using System.Reflection;
using HarmonyLib;

namespace QudJP.Patches;

[HarmonyPatch]
public static class OptionsLocalizationPatch
{
    private const string TargetTypeName = "Qud.UI.OptionsScreen";

    [HarmonyTargetMethod]
    private static MethodBase? TargetMethod()
    {
        return AccessTools.Method(TargetTypeName + ":Show");
    }

    public static void Postfix(object __instance)
    {
        if (__instance is null)
        {
            return;
        }

        var type = __instance.GetType();

        var menuItemsField = AccessTools.Field(type, "menuItems");
        UITextSkinTranslationPatch.TranslateStringFieldsInCollection(menuItemsField?.GetValue(__instance), "Title", "HelpText");

        var filteredMenuItemsField = AccessTools.Field(type, "filteredMenuItems");
        UITextSkinTranslationPatch.TranslateStringFieldsInCollection(filteredMenuItemsField?.GetValue(__instance), "Title", "HelpText");

        var defaultMenuOptionsField = AccessTools.Field(type, "defaultMenuOptions");
        UITextSkinTranslationPatch.TranslateStringFieldsInCollection(defaultMenuOptionsField?.GetValue(null), "Description");
    }
}
