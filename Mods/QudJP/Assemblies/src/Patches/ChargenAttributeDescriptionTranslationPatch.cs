using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;

namespace QudJP.Patches;

[HarmonyPatch]
public static class ChargenAttributeDescriptionTranslationPatch
{
    private const string Context = nameof(ChargenAttributeDescriptionTranslationPatch);
    private const string BeforeGetBaseAttributesId = "BeforeGetBaseAttributes";

    [HarmonyTargetMethod]
    private static MethodBase? TargetMethod()
    {
        var targetType = GameTypeResolver.FindType(
            "XRL.CharacterBuilds.Qud.QudGenotypeModule",
            "QudGenotypeModule");
        if (targetType is null)
        {
            Trace.TraceError("QudJP: {0} target type not found.", Context);
            return null;
        }

        var method = AccessTools.Method(targetType, "handleUIEvent", new[] { typeof(string), typeof(object) });
        if (method is null)
        {
            Trace.TraceError("QudJP: {0}.handleUIEvent(string, object) not found.", Context);
        }

        return method;
    }

    public static void Postfix(string ID, object? Element)
    {
        try
        {
            if (!string.Equals(ID, BeforeGetBaseAttributesId, StringComparison.Ordinal)
                || Element is not IEnumerable attributes)
            {
                return;
            }

            foreach (var attribute in attributes)
            {
                if (attribute is null)
                {
                    continue;
                }

                TranslateDescription(attribute);
            }
        }
        catch (Exception ex)
        {
            Trace.TraceError("QudJP: {0}.Postfix failed: {1}", Context, ex);
        }
    }

    private static void TranslateDescription(object attribute)
    {
        if (!TryGetStringMemberValue(attribute, "Description", out var source)
            || string.IsNullOrEmpty(source)
            || !Translator.TryGetTranslation(source!, out var translated)
            || string.Equals(translated, source, StringComparison.Ordinal))
        {
            return;
        }

        DynamicTextObservability.RecordTransform(Context, "Chargen.AttributeDescription", source!, translated);
        SetStringMemberValue(attribute, "Description", translated);
    }

    private static bool TryGetStringMemberValue(object target, string memberName, out string? value)
    {
        value = null;
        var type = target.GetType();

        var field = AccessTools.Field(type, memberName);
        if (field is not null && field.FieldType == typeof(string))
        {
            value = field.GetValue(target) as string;
            return true;
        }

        var property = AccessTools.Property(type, memberName);
        if (property is not null && property.CanRead && property.PropertyType == typeof(string))
        {
            value = property.GetValue(target) as string;
            return true;
        }

        return false;
    }

    private static void SetStringMemberValue(object target, string memberName, string value)
    {
        var type = target.GetType();

        var field = AccessTools.Field(type, memberName);
        if (field is not null && field.FieldType == typeof(string))
        {
            field.SetValue(target, value);
            return;
        }

        var property = AccessTools.Property(type, memberName);
        if (property is not null && property.CanWrite && property.PropertyType == typeof(string))
        {
            property.SetValue(target, value);
        }
    }
}
