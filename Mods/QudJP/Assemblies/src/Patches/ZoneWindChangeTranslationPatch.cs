using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using HarmonyLib;

namespace QudJP.Patches;

[HarmonyPatch]
public static class ZoneWindChangeTranslationPatch
{
    private const string Context = nameof(ZoneWindChangeTranslationPatch);
    private const string Family = "Zone.WindChange";

    private static readonly Regex DirectionChangePattern = CreatePattern(
        "^The wind changes direction from the (?<from>.+?) to the (?<to>.+?)\\.$");

    private static readonly Regex BeginsWithDirectionPattern = CreatePattern(
        "^The wind begins blowing at (?<speed>.+?) from the (?<direction>.+?)\\.$");

    private static readonly Regex IntensifiesWithDirectionPattern = CreatePattern(
        "^The wind intensifies to (?<speed>.+?), blowing from the (?<direction>.+?)\\.$");

    private static readonly Regex CalmsWithDirectionPattern = CreatePattern(
        "^The wind calms to (?<speed>.+?), blowing from the (?<direction>.+?)\\.$");

    private static readonly Regex BeginsPattern = CreatePattern(
        "^The wind begins blowing at (?<speed>.+?)\\.$");

    private static readonly Regex IntensifiesPattern = CreatePattern(
        "^The wind intensifies to (?<speed>.+?)\\.$");

    private static readonly Regex CalmsPattern = CreatePattern(
        "^The wind calms to (?<speed>.+?)\\.$");

    [ThreadStatic]
    private static int activeDepth;

    [HarmonyTargetMethod]
    private static MethodBase? TargetMethod()
    {
        var zoneType = AccessTools.TypeByName("XRL.World.Zone");
        if (zoneType is null)
        {
            Trace.TraceError("QudJP: {0} target type not found.", Context);
            return null;
        }

        var method = AccessTools.Method(zoneType, "WindChange", [typeof(long)]);
        if (method is null)
        {
            Trace.TraceError("QudJP: {0}.WindChange(long) not found.", Context);
        }

        return method;
    }

    public static void Prefix()
    {
        try
        {
            activeDepth++;
        }
        catch (Exception ex)
        {
            Trace.TraceError("QudJP: {0}.Prefix failed: {1}", Context, ex);
        }
    }

    public static Exception? Finalizer(Exception? __exception)
    {
        try
        {
            if (activeDepth > 0)
            {
                activeDepth--;
            }
        }
        catch (Exception ex)
        {
            Trace.TraceError("QudJP: {0}.Finalizer failed: {1}", Context, ex);
        }

        return __exception;
    }

    internal static bool TryTranslateQueuedMessage(ref string message, string? color)
    {
        _ = color;

        if (activeDepth <= 0 || string.IsNullOrEmpty(message))
        {
            return false;
        }

        if (!TryTranslate(message, out var translated))
        {
            return false;
        }

        message = translated;
        return true;
    }

    private static bool TryTranslate(string source, out string translated)
    {
        if (MessageFrameTranslator.TryStripDirectTranslationMarker(source, out _))
        {
            translated = source;
            return false;
        }

        var (stripped, spans) = ColorAwareTranslationComposer.Strip(source);
        if (TryBuildExact(source, stripped, spans, "The wind changes direction.", "風向きが変わった。", out translated)
            || TryBuildExact(source, stripped, spans, "The wind becomes still.", "風が静まった。", out translated)
            || TryBuild(
                source,
                stripped,
                spans,
                DirectionChangePattern,
                BuildDirectionChange,
                out translated)
            || TryBuild(
                source,
                stripped,
                spans,
                BeginsWithDirectionPattern,
                BuildBeginsWithDirection,
                out translated)
            || TryBuild(
                source,
                stripped,
                spans,
                IntensifiesWithDirectionPattern,
                BuildIntensifiesWithDirection,
                out translated)
            || TryBuild(
                source,
                stripped,
                spans,
                CalmsWithDirectionPattern,
                BuildCalmsWithDirection,
                out translated)
            || TryBuild(
                source,
                stripped,
                spans,
                BeginsPattern,
                BuildBegins,
                out translated)
            || TryBuild(
                source,
                stripped,
                spans,
                IntensifiesPattern,
                BuildIntensifies,
                out translated)
            || TryBuild(
                source,
                stripped,
                spans,
                CalmsPattern,
                BuildCalms,
                out translated))
        {
            DynamicTextObservability.RecordTransform(Context, Family, source, translated);
            return true;
        }

        translated = source;
        return false;
    }

    private static bool TryBuildExact(
        string source,
        string stripped,
        IReadOnlyList<ColorSpan> spans,
        string exact,
        string visible,
        out string translated)
    {
        if (!string.Equals(stripped, exact, StringComparison.Ordinal))
        {
            translated = source;
            return false;
        }

        translated = RestoreBoundary(stripped, spans, visible);
        return true;
    }

    private static bool TryBuild(
        string source,
        string stripped,
        IReadOnlyList<ColorSpan> spans,
        Regex pattern,
        Func<Match, IReadOnlyList<ColorSpan>, string?> build,
        out string translated)
    {
        var match = pattern.Match(stripped);
        if (!match.Success)
        {
            translated = source;
            return false;
        }

        var visible = build(match, spans);
        if (visible is null)
        {
            translated = source;
            return false;
        }

        translated = RestoreBoundary(stripped, spans, visible);
        return true;
    }

    private static string RestoreBoundary(
        string stripped,
        IReadOnlyList<ColorSpan> spans,
        string visible)
    {
        return ColorAwareTranslationComposer.RestoreWholeSourceBoundaryWrappersPreservingTranslatedOwnership(
            visible,
            spans,
            stripped.Length);
    }

    private static string? BuildDirectionChange(Match match, IReadOnlyList<ColorSpan> spans)
    {
        var from = Direction(match, spans, "from");
        var to = Direction(match, spans, "to");
        return from is null || to is null
            ? null
            : "風向きが" + from + "から" + to + "へ変わった。";
    }

    private static string? BuildBeginsWithDirection(Match match, IReadOnlyList<ColorSpan> spans)
    {
        var direction = Direction(match, spans, "direction");
        var speed = WindSpeed(match, spans, "speed");
        return direction is null || speed is null
            ? null
            : direction + "から" + speed + "が吹き始めた。";
    }

    private static string? BuildIntensifiesWithDirection(Match match, IReadOnlyList<ColorSpan> spans)
    {
        var direction = Direction(match, spans, "direction");
        var speed = WindSpeed(match, spans, "speed");
        return direction is null || speed is null
            ? null
            : direction + "から吹く風が" + speed + "まで強まった。";
    }

    private static string? BuildCalmsWithDirection(Match match, IReadOnlyList<ColorSpan> spans)
    {
        var direction = Direction(match, spans, "direction");
        var speed = WindSpeed(match, spans, "speed");
        return direction is null || speed is null
            ? null
            : direction + "から吹く風が" + speed + "まで弱まった。";
    }

    private static string? BuildBegins(Match match, IReadOnlyList<ColorSpan> spans)
    {
        var speed = WindSpeed(match, spans, "speed");
        return speed is null ? null : speed + "が吹き始めた。";
    }

    private static string? BuildIntensifies(Match match, IReadOnlyList<ColorSpan> spans)
    {
        var speed = WindSpeed(match, spans, "speed");
        return speed is null ? null : "風が" + speed + "まで強まった。";
    }

    private static string? BuildCalms(Match match, IReadOnlyList<ColorSpan> spans)
    {
        var speed = WindSpeed(match, spans, "speed");
        return speed is null ? null : "風が" + speed + "まで弱まった。";
    }

    private static string? Direction(Match match, IReadOnlyList<ColorSpan> spans, string groupName)
    {
        var group = match.Groups[groupName];
        return TranslateCaptured(group, spans, TranslateDirection);
    }

    private static string? WindSpeed(Match match, IReadOnlyList<ColorSpan> spans, string groupName)
    {
        var group = match.Groups[groupName];
        return TranslateCaptured(group, spans, TranslateWindSpeed);
    }

    private static string? TranslateCaptured(
        Group group,
        IReadOnlyList<ColorSpan> spans,
        Func<string, string?> translate)
    {
        var translated = translate(group.Value);
        return translated is null
            ? null
            : ColorAwareTranslationComposer.RestoreCapture(translated, spans, group).Trim();
    }

    private static string? TranslateDirection(string direction)
    {
        return direction switch
        {
            "north" => "北",
            "south" => "南",
            "east" => "東",
            "west" => "西",
            "northeast" => "北東",
            "northwest" => "北西",
            "southeast" => "南東",
            "southwest" => "南西",
            _ => null,
        };
    }

    private static string? TranslateWindSpeed(string speed)
    {
        return speed switch
        {
            "a very gentle breeze" => "ごく弱い風",
            "a gentle breeze" => "弱い風",
            "a moderate breeze" => "ほどよい風",
            "a fresh breeze" => "やや強い風",
            "a strong breeze" => "強い風",
            "near gale intensity" => "疾強風に近い風",
            "gale intensity" => "疾強風",
            "strong gale intensity" => "強い疾強風",
            "storm intensity" => "暴風",
            "violent storm intensity" => "激しい暴風",
            "hurricane intensity" => "ハリケーン級の風",
            _ => null,
        };
    }

    private static Regex CreatePattern(string pattern)
    {
        return new Regex(pattern, RegexOptions.CultureInvariant | RegexOptions.Compiled);
    }
}
