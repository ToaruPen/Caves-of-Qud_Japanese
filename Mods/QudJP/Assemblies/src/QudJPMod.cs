using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;

namespace QudJP;

public static class QudJPMod
{
    private static int isInitialized;

    public static void Init()
    {
        Initialize();
    }

    public static void Reset()
    {
        Initialize();
    }

    private static void Initialize()
    {
        if (Interlocked.Exchange(ref isInitialized, 1) == 1)
        {
            return;
        }

        FontManager.Initialize();
        ApplyHarmonyPatches();
    }

    private static void ApplyHarmonyPatches()
    {
        var harmony = CreateHarmony("com.qudjp.localization");
        if (harmony is null)
        {
            throw new InvalidOperationException(
                "QudJP: Harmony runtime not available. The mod cannot function without Harmony.");
        }

        InvokePatchAll(harmony);
    }

    private static object? CreateHarmony(string harmonyId)
    {
        var harmonyType = ResolveHarmonyType();
        if (harmonyType is null)
        {
            Trace.TraceError("QudJP: HarmonyLib.Harmony type not found in any loaded assembly.");
            return null;
        }

        var constructor = harmonyType.GetConstructor(new[] { typeof(string) });
        if (constructor is null)
        {
            Trace.TraceError("QudJP: HarmonyLib.Harmony(string) constructor not found.");
            return null;
        }

        return constructor.Invoke(new object[] { harmonyId });
    }

    private static void InvokePatchAll(object harmony)
    {
        var harmonyType = harmony.GetType();
        var patchAllWithoutArgs = harmonyType.GetMethod(
            "PatchAll",
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null);

        if (patchAllWithoutArgs is not null)
        {
            patchAllWithoutArgs.Invoke(harmony, null);
            return;
        }

        var patchAllWithAssembly = harmonyType.GetMethod(
            "PatchAll",
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            types: new[] { typeof(Assembly) },
            modifiers: null);

        if (patchAllWithAssembly is not null)
        {
            patchAllWithAssembly.Invoke(harmony, new object[] { Assembly.GetExecutingAssembly() });
            return;
        }

        throw new MissingMethodException(harmonyType.FullName, "PatchAll");
    }

    private static Type? ResolveHarmonyType()
    {
        var typeFrom0Harmony = Type.GetType("HarmonyLib.Harmony, 0Harmony", throwOnError: false);
        if (typeFrom0Harmony is not null)
        {
            return typeFrom0Harmony;
        }

        Trace.TraceWarning("QudJP: Harmony type was not found in 0Harmony. Trying HarmonyLib assembly name.");
        return Type.GetType("HarmonyLib.Harmony, HarmonyLib", throwOnError: false);
    }
}
