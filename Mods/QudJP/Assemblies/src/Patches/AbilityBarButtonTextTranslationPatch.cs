using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace QudJP.Patches;

[HarmonyPatch]
public static class AbilityBarButtonTextTranslationPatch
{
    private const string Context = nameof(AbilityBarButtonTextTranslationPatch);

    [HarmonyTargetMethod]
    private static MethodBase? TargetMethod()
    {
        var targetType = GameTypeResolver.FindType("Qud.UI.AbilityBar", "AbilityBar");
        if (targetType is null)
        {
            Trace.TraceError("QudJP: {0} target type not found.", Context);
            return null;
        }

        var method = AccessTools.Method(targetType, "Update", Type.EmptyTypes);
        if (method is null)
        {
            Trace.TraceError("QudJP: {0}.Update not found.", Context);
        }

        return method;
    }

    public static void Postfix(object __instance)
    {
        try
        {
            TranslateAbilityButtons(__instance);
        }
        catch (Exception ex)
        {
            Trace.TraceError("QudJP: {0}.Postfix failed: {1}", Context, ex);
        }
    }

    private static void TranslateAbilityButtons(object instance)
    {
        var buttonsField = AccessTools.Field(instance.GetType(), "AbilityButtons");
        if (buttonsField?.GetValue(instance) is not IEnumerable buttons)
        {
            return;
        }

        foreach (var buttonObject in buttons.Cast<object?>())
        {
            var component = ResolveButtonComponent(buttonObject);
            if (component is null)
            {
                continue;
            }

            var textObject = AccessTools.Field(component.GetType(), "Text")?.GetValue(component);
            var current = UITextSkinReflectionAccessor.GetCurrentText(textObject, Context);
            if (string.IsNullOrEmpty(current))
            {
                continue;
            }

            var route = ObservabilityHelpers.ComposeContext(Context, "field=AbilityButtons.Text");
            if (!TryTranslateAbilityButtonText(current!, route, out var translated)
                || string.Equals(current, translated, StringComparison.Ordinal))
            {
                continue;
            }

            if (UITextSkinReflectionAccessor.SetCurrentText(textObject, translated, Context))
            {
                DynamicTextObservability.RecordTransform(route, "AbilityBar.ButtonText", current!, translated);
            }
        }
    }

    private static object? ResolveButtonComponent(object? buttonObject)
    {
        if (buttonObject is null)
        {
            return null;
        }

        if (AccessTools.Field(buttonObject.GetType(), "Text") is not null)
        {
            return buttonObject;
        }

        var componentType = GameTypeResolver.FindType("Qud.UI.AbilityBarButton", "AbilityBarButton");
        if (componentType is null)
        {
            return null;
        }

        var getComponentByType = AccessTools.Method(buttonObject.GetType(), "GetComponent", new[] { typeof(Type) });
        if (getComponentByType is not null)
        {
            return getComponentByType.Invoke(buttonObject, new object[] { componentType });
        }

        var getComponentGeneric = buttonObject.GetType()
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(static method =>
                string.Equals(method.Name, "GetComponent", StringComparison.Ordinal)
                && method.IsGenericMethodDefinition
                && method.GetParameters().Length == 0);
        return getComponentGeneric?.MakeGenericMethod(componentType).Invoke(buttonObject, Array.Empty<object>());
    }

    private static bool TryTranslateAbilityButtonText(string source, string route, out string translated)
    {
        var suffixIndex = source.IndexOf(" {{", StringComparison.Ordinal);
        var nameSegment = suffixIndex >= 0 ? source.Substring(0, suffixIndex) : source;
        var suffix = suffixIndex >= 0 ? source.Substring(suffixIndex) : string.Empty;

        var changed = false;
        var translatedName = TranslateNameSegment(nameSegment, route, out var nameChanged);
        changed |= nameChanged;

        var translatedSuffix = TranslateSuffix(suffix, out var suffixChanged);
        changed |= suffixChanged;

        translated = translatedName + translatedSuffix;
        return changed;
    }

    private static string TranslateNameSegment(string source, string route, out bool changed)
    {
        var (stripped, spans) = ColorAwareTranslationComposer.Strip(source);
        var translated = StringHelpers.TranslateExactOrLowerAscii(stripped);
        if (translated is null)
        {
            translated = GetDisplayNameRouteTranslator.TranslatePreservingColors(
                source,
                ObservabilityHelpers.ComposeContext(route, "segment=name"));
            changed = !string.Equals(translated, source, StringComparison.Ordinal);
            return translated;
        }

        var restored = spans.Count == 0 ? translated : ColorAwareTranslationComposer.Restore(translated, spans);
        changed = !string.Equals(restored, source, StringComparison.Ordinal);
        return restored;
    }

    private static string TranslateSuffix(string source, out bool changed)
    {
        var translated = source;
        translated = ReplaceExactToken(translated, "[disabled]");
        translated = ReplaceExactToken(translated, "on");
        translated = ReplaceExactToken(translated, "off");
        changed = !string.Equals(translated, source, StringComparison.Ordinal);
        return translated;
    }

    private static string ReplaceExactToken(string source, string token)
    {
        var translated = StringHelpers.TranslateExactOrLowerAscii(token);
        return translated is null
            ? source
            : source.Replace(token, translated);
    }
}
