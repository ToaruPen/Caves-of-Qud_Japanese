using System;
using System.Reflection;
using HarmonyLib;

namespace QudJP;

internal static class GameTypeResolver
{
    internal static Type? FindType(string fullTypeName, string simpleTypeName)
    {
        var byFullName = AccessTools.TypeByName(fullTypeName);
        if (byFullName is not null)
        {
            return byFullName;
        }

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        for (var assemblyIndex = 0; assemblyIndex < assemblies.Length; assemblyIndex++)
        {
            Type[] types;
            try
            {
                types = assemblies[assemblyIndex].GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = Array.FindAll(ex.Types, static type => type is not null)!;
            }

            for (var typeIndex = 0; typeIndex < types.Length; typeIndex++)
            {
                if (types[typeIndex].Name == simpleTypeName)
                {
                    return types[typeIndex];
                }
            }
        }

        return null;
    }
}
