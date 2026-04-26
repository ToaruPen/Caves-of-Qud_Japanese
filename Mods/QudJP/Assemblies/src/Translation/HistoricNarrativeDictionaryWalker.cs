using System;
using System.Collections.Generic;
using System.Linq;
#if HAS_GAME_DLL
using HistoryKit;
#endif

namespace QudJP;

/// <summary>
/// Walks HistoricEvent.eventProperties (direct mutation) and HistoricEntity
/// (mutation events via SetEntityPropertyAtCurrentYear / MutateListPropertyAtCurrentYear)
/// applying <see cref="HistoricNarrativeTextTranslator"/> only to allowlisted keys.
/// </summary>
internal static class HistoricNarrativeDictionaryWalker
{
    internal static readonly HashSet<string> EventPropertyAllowlist = new(StringComparer.Ordinal)
    {
        "gospel",
        "tombInscription",
    };

    internal static readonly HashSet<string> EntityPropertyAllowlist = new(StringComparer.Ordinal)
    {
        "proverb",
        "defaultSacredThing",
        "defaultProfaneThing",
    };

    internal static readonly HashSet<string> EntityListPropertyAllowlist = new(StringComparer.Ordinal)
    {
        "Gospels",
        "sacredThings",
        "profaneThings",
        "immigrant_dialogWhy_Q",
        "immigrant_dialogWhy_A",
        "pet_dialogWhy_Q",
    };

#pragma warning disable S1144, CA1823 // field used in Task 4 (Green) implementation
    private const string GospelEventIdSeparator = "|";
#pragma warning restore S1144, CA1823

    /// <summary>
    /// L1-testable. Splits <paramref name="raw"/> on <see cref="GospelEventIdSeparator"/>
    /// and translates only the prose portion, preserving the trailing <c>|eventId</c> verbatim.
    /// </summary>
    internal static string TranslateGospelEntry(string raw, string? context = null)
    {
        throw new NotImplementedException();
    }

    /// <summary>L1-testable. Mutates the dict in place per the event-property allowlist.</summary>
    internal static void TranslateEventPropertiesDict(IDictionary<string, string> properties, string? context = null)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// L1-testable. Reads current snapshot via <paramref name="readProperty"/> / <paramref name="readList"/>,
    /// translates each allowlisted value, writes back via <paramref name="writeProperty"/> /
    /// <paramref name="mutateList"/>. The list mutation callback is invoked only when at least one
    /// element changed (sequence equality guard).
    /// </summary>
    internal static void TranslateEntityViaCallbacks(
        Func<string, string?> readProperty,
        Func<string, IReadOnlyList<string>?> readList,
        Action<string, string> writeProperty,
        Action<string, Func<string, string>> mutateList,
        string? context = null)
    {
        throw new NotImplementedException();
    }

#if HAS_GAME_DLL
    internal static void TranslateEventProperties(HistoricEvent ev, string? context = null)
    {
        throw new NotImplementedException();
    }

    internal static void TranslateEntity(HistoricEntity entity, string? context = null)
    {
        throw new NotImplementedException();
    }
#endif
}
