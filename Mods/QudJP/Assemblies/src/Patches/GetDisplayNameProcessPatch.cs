using System;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;

namespace QudJP.Patches;

[HarmonyPatch]
public static class GetDisplayNameProcessPatch
{
    private const string TargetTypeName = "XRL.World.GetDisplayNameEvent";

    [HarmonyTargetMethod]
    private static MethodBase? TargetMethod()
    {
        var gameObjectType = AccessTools.TypeByName("XRL.World.GameObject");
        if (gameObjectType is not null)
        {
            var method = AccessTools.Method(TargetTypeName + ":ProcessFor", new[] { gameObjectType, typeof(bool) });
            if (method is not null)
            {
                return method;
            }
        }

        foreach (var type in AccessTools.AllTypes())
        {
            if (type is null)
            {
                continue;
            }

            var fullName = type.FullName;
            if (!string.Equals(fullName, TargetTypeName, StringComparison.Ordinal)
                && !string.Equals(type.Name, "GetDisplayNameEvent", StringComparison.Ordinal))
            {
                continue;
            }

            var methods = AccessTools.GetDeclaredMethods(type);
            for (var index = 0; index < methods.Count; index++)
            {
                var candidate = methods[index];
                if (!string.Equals(candidate.Name, "ProcessFor", StringComparison.Ordinal)
                    || candidate.ReturnType != typeof(string))
                {
                    continue;
                }

                var parameters = candidate.GetParameters();
                if (parameters.Length == 2 && parameters[1].ParameterType == typeof(bool))
                {
                    return candidate;
                }
            }
        }

        Trace.TraceError("QudJP: Failed to resolve GetDisplayNameEvent.ProcessFor(GameObject,bool). Patch will not apply.");
        return null;
    }

    public static void Postfix(ref string __result)
    {
        try
        {
            if (string.IsNullOrEmpty(__result))
            {
                return;
            }

            __result = TranslatePreservingColors(__result);
        }
        catch (Exception ex)
        {
            Trace.TraceError("QudJP: GetDisplayNameProcessPatch.Postfix failed: {0}", ex);
        }
    }

    private static string TranslatePreservingColors(string source)
    {
        var (stripped, spans) = ColorCodePreserver.Strip(source);
        if (stripped.Length == 0)
        {
            return source;
        }

        var translated = Translator.Translate(stripped);
        return ColorCodePreserver.Restore(translated, spans);
    }
}
