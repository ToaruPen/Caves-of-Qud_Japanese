using System;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;

namespace QudJP.Patches;

internal static class GrammarPatchTarget
{
    internal const string TypeName = "XRL.Language.Grammar";
}

internal static class GrammarPatchHelpers
{
    internal static string BuildJapaneseList(List<string> items, string conjunction)
    {
        if (items.Count == 0)
        {
            return string.Empty;
        }

        if (items.Count == 1)
        {
            return items[0];
        }

        if (items.Count == 2)
        {
            return items[0] + conjunction + items[1];
        }

        var result = items[0];
        for (var index = 1; index < items.Count - 1; index++)
        {
            result += "、" + items[index];
        }

        return result + "、" + conjunction + items[items.Count - 1];
    }

    internal static List<string> SplitSentenceList(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return new List<string>();
        }

        var normalized = text
            .Replace("、", ",")
            .Replace(", and ", ", ")
            .Replace(" and ", ", ");

        var fragments = normalized.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        var result = new List<string>(fragments.Length);
        for (var index = 0; index < fragments.Length; index++)
        {
            var trimmed = fragments[index].Trim();
            if (trimmed.Length > 0)
            {
                result.Add(trimmed);
            }
        }

        return result;
    }
}

[HarmonyPatch]
public static class GrammarAPatch
{
    [HarmonyTargetMethod]
    private static MethodBase? TargetMethod()
    {
        var method = AccessTools.Method(GrammarPatchTarget.TypeName + ":A", new[] { typeof(string), typeof(bool) });
        if (method is null)
        {
            Trace.TraceError("QudJP: Failed to resolve Grammar.A(string, bool). Patch will not apply.");
        }

        return method;
    }

    public static bool Prefix(string Word, bool Capitalize, ref string __result)
    {
        try
        {
            _ = Capitalize;
            __result = Word;
            return false;
        }
        catch (Exception ex)
        {
            Trace.TraceError("QudJP: GrammarAPatch.Prefix failed: {0}", ex);
            return true;
        }
    }
}

[HarmonyPatch]
public static class GrammarPluralizePatch
{
    [HarmonyTargetMethod]
    private static MethodBase? TargetMethod()
    {
        var method = AccessTools.Method(GrammarPatchTarget.TypeName + ":Pluralize", new[] { typeof(string) });
        if (method is null)
        {
            Trace.TraceError("QudJP: Failed to resolve Grammar.Pluralize(string). Patch will not apply.");
        }

        return method;
    }

    public static bool Prefix(string word, ref string __result)
    {
        try
        {
            __result = word;
            return false;
        }
        catch (Exception ex)
        {
            Trace.TraceError("QudJP: GrammarPluralizePatch.Prefix failed: {0}", ex);
            return true;
        }
    }
}

[HarmonyPatch]
public static class GrammarMakePossessivePatch
{
    [HarmonyTargetMethod]
    private static MethodBase? TargetMethod()
    {
        var method = AccessTools.Method(GrammarPatchTarget.TypeName + ":MakePossessive", new[] { typeof(string) });
        if (method is null)
        {
            Trace.TraceError("QudJP: Failed to resolve Grammar.MakePossessive(string). Patch will not apply.");
        }

        return method;
    }

    public static bool Prefix(string word, ref string __result)
    {
        try
        {
            __result = word.EndsWith("の", StringComparison.Ordinal) ? word : word + "の";
            return false;
        }
        catch (Exception ex)
        {
            Trace.TraceError("QudJP: GrammarMakePossessivePatch.Prefix failed: {0}", ex);
            return true;
        }
    }
}

[HarmonyPatch]
public static class GrammarMakeAndListPatch
{
    [HarmonyTargetMethod]
    private static MethodBase? TargetMethod()
    {
        try
        {
            var method = AccessTools.Method(GrammarPatchTarget.TypeName + ":MakeAndList", new[] { typeof(IReadOnlyList<string>), typeof(bool) });
            if (method is null)
            {
                Trace.TraceError("QudJP: Failed to resolve Grammar.MakeAndList(IReadOnlyList<string>, bool). Patch will not apply.");
            }

            return method;
        }
        catch (Exception ex)
        {
            Trace.TraceError("QudJP: GrammarMakeAndListPatch.TargetMethod failed: {0}", ex);
            return null;
        }
    }

    public static bool Prefix(IEnumerable<string> __0, bool __1, ref string __result)
    {
        try
        {
            _ = __1;
            __result = GrammarPatchHelpers.BuildJapaneseList(new List<string>(__0), "と");
            return false;
        }
        catch (Exception ex)
        {
            Trace.TraceError("QudJP: GrammarMakeAndListPatch.Prefix failed: {0}", ex);
            return true;
        }
    }
}

[HarmonyPatch]
public static class GrammarMakeOrListPatch
{
    [HarmonyTargetMethods]
    private static IEnumerable<MethodBase> TargetMethods()
    {
        var any = false;

        MethodBase?[] candidates =
        {
            AccessTools.Method(GrammarPatchTarget.TypeName + ":MakeOrList", new[] { typeof(string[]), typeof(bool) }),
            AccessTools.Method(GrammarPatchTarget.TypeName + ":MakeOrList", new[] { typeof(List<string>), typeof(bool) }),
        };

        for (var index = 0; index < candidates.Length; index++)
        {
            var method = candidates[index];
            if (method is null)
            {
                continue;
            }

            any = true;
            yield return method;
        }

        if (!any)
        {
            Trace.TraceError("QudJP: Failed to resolve Grammar.MakeOrList(string[]/List<string>, bool). Patch will not apply.");
        }
    }

    public static bool Prefix(IEnumerable<string> __0, bool __1, ref string __result)
    {
        try
        {
            _ = __1;
            __result = GrammarPatchHelpers.BuildJapaneseList(new List<string>(__0), "または");
            return false;
        }
        catch (Exception ex)
        {
            Trace.TraceError("QudJP: GrammarMakeOrListPatch.Prefix failed: {0}", ex);
            return true;
        }
    }
}

[HarmonyPatch]
public static class GrammarSplitOfSentenceListPatch
{
    [HarmonyTargetMethod]
    private static MethodBase? TargetMethod()
    {
        try
        {
            var method = AccessTools.Method(GrammarPatchTarget.TypeName + ":SplitOfSentenceList", new[] { typeof(string) });
            if (method is null)
            {
                Trace.TraceError("QudJP: Failed to resolve Grammar.SplitOfSentenceList(string). Patch will not apply.");
            }

            return method;
        }
        catch (Exception ex)
        {
            Trace.TraceError("QudJP: GrammarSplitOfSentenceListPatch.TargetMethod failed: {0}", ex);
            return null;
        }
    }

    public static bool Prefix(string Text, ref List<string> __result)
    {
        try
        {
            __result = GrammarPatchHelpers.SplitSentenceList(Text);
            return false;
        }
        catch (Exception ex)
        {
            Trace.TraceError("QudJP: GrammarSplitOfSentenceListPatch.Prefix failed: {0}", ex);
            return true;
        }
    }
}

[HarmonyPatch]
public static class GrammarInitCapsPatch
{
    [HarmonyTargetMethod]
    private static MethodBase? TargetMethod()
    {
        try
        {
            var method = AccessTools.Method(GrammarPatchTarget.TypeName + ":InitCaps", new[] { typeof(string) });
            if (method is null)
            {
                Trace.TraceError("QudJP: Failed to resolve Grammar.InitCaps(string). Patch will not apply.");
            }

            return method;
        }
        catch (Exception ex)
        {
            Trace.TraceError("QudJP: GrammarInitCapsPatch.TargetMethod failed: {0}", ex);
            return null;
        }
    }

    public static bool Prefix(string Text, ref string __result)
    {
        try
        {
            __result = Text;
            return false;
        }
        catch (Exception ex)
        {
            Trace.TraceError("QudJP: GrammarInitCapsPatch.Prefix failed: {0}", ex);
            return true;
        }
    }
}

[HarmonyPatch]
public static class GrammarCardinalNumberPatch
{
    [HarmonyTargetMethod]
    private static MethodBase? TargetMethod()
    {
        try
        {
            var method = AccessTools.Method(GrammarPatchTarget.TypeName + ":CardinalNumber", new[] { typeof(int) });
            if (method is null)
            {
                Trace.TraceError("QudJP: Failed to resolve Grammar.CardinalNumber(int). Patch will not apply.");
            }

            return method;
        }
        catch (Exception ex)
        {
            Trace.TraceError("QudJP: GrammarCardinalNumberPatch.TargetMethod failed: {0}", ex);
            return null;
        }
    }

    public static bool Prefix(int Number, ref string __result)
    {
        try
        {
            __result = Number.ToString(CultureInfo.InvariantCulture);
            return false;
        }
        catch (Exception ex)
        {
            Trace.TraceError("QudJP: GrammarCardinalNumberPatch.Prefix failed: {0}", ex);
            return true;
        }
    }
}
