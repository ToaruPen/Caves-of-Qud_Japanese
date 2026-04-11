# 2026-04-11 EmitMessage / AddPlayerMessage batch 01

## Scope

This note covers the unresolved message buckets identified in `docs/reports/2026-04-11-fixed-leaf-owner-triage.md`:

- `AddPlayerMessage` — 403
- `EmitMessage` — 214

The goal is to separate sink-observed families from producer-owned families before deeper triage.

## AddPlayerMessage reading

### Current sink patch

- `Mods/QudJP/Assemblies/src/Patches/MessageLogPatch.cs:11-45`
  - patches `MessageQueue.AddPlayerMessage`
  - strips direct markers if already owned upstream
  - otherwise records `SinkObservation.LogUnclaimed(...)`
  - returns without translating the message itself

### Why this stays observation-only

- `docs/archive/source-first-design.md:19-34,64-71`
  - treats `AddPlayerMessage` as one of the largest sink families, not as a trustworthy permanent owner
  - explicitly reserves generic popup/message wrappers and blueprint/tag-driven strings for upstream ownership or runtime evidence
- `docs/RULES.md:56-63`
  - says dynamic or sink-observed routes should be handled by C# route owners, not compensating dictionary entries at the sink

### Current behavior proven by tests

- `Mods/QudJP/Assemblies/QudJP.Tests/L2/MessageLogPatchTests.cs:47-69`
  - hit message remains untranslated even if a matching pattern exists
- `Mods/QudJP/Assemblies/QudJP.Tests/L2/MessageLogPatchTests.cs:71-93,147-168`
  - preserves color and other arguments while remaining observation-only
- `Mods/QudJP/Assemblies/QudJP.Tests/L2/MessageLogPatchTests.cs:176-204,206-224`
  - status predicate and weapon combat examples also pass through unchanged

### Producer-owned families already split out of the sink

Two important `AddPlayerMessage` subfamilies are already treated as upstream-owned overlays rather than generic sink ownership:

- `PhysicsEnterCellPassByTranslationPatch.cs:14-43`
  - intercepts pass-by messages at the queue edge and rewrites them through `PreparePassByMessage(...)`
- `ZoneManagerSetActiveZoneTranslationPatch.cs:19-90` + `ZoneManagerSetActiveZoneMessageQueuePatch.cs:11-32`
  - claims zone-banner messages during `SetActiveZone(...)` and rewrites them through `PrepareZoneBannerMessage(...)`

Tests prove both happen **before** the generic `MessageLogPatch` sink:

- `PhysicsEnterCellPassByTranslationPatchTests.cs:47-95`
- `ZoneManagerSetActiveZoneTranslationPatchTests.cs:48-172`
- target binding is verified in `TargetMethodResolutionTests.cs:166-181`

### Verdict for AddPlayerMessage

`AddPlayerMessage` should stay a **sink-observed umbrella**, not become the permanent owner of all message-log text.

The right split is:

1. keep `MessageLogPatch` observation-only
2. peel off producer-owned subfamilies (like pass-by / zone-banner / other explicit queue overlays)
3. treat the remaining generic log traffic as evidence, not as a compensating translation home

Operationally, this means unresolved `AddPlayerMessage` rows should first be asked, "which upstream producer family owns this?" rather than, "which sink regex should absorb it?"

## EmitMessage reading

### Current producer-owned route

- `Mods/QudJP/Assemblies/src/Patches/GameObjectEmitMessageTranslationPatch.cs:17-104`
  - directly wraps both `GameObject.EmitMessage(...)` and `Messaging.EmitMessage(...)`
  - tracks `activeDepth`
  - claims queued messages through `TryTranslateQueuedMessage(...)`

### Queue integration

- `Mods/QudJP/Assemblies/src/Patches/CombatAndLogMessageQueuePatch.cs:25-51`
  - queues many owner-specific translators
  - includes `GameObjectEmitMessageTranslationPatch.TryTranslateQueuedMessage(...)` in the OR chain

### Shared helper / dictionary side

- `Mods/QudJP/Assemblies/src/Patches/MessageLogProducerTranslationHelpers.cs:215-251`
  - runs `MessagePatternTranslator.Translate(...)`
  - marks Japanese or matched output as direct to avoid downstream double processing
- `Mods/QudJP/Localization/Dictionaries/messages.ja.json:21-44,106-179`
  - repository patterns already include many `route = emit-message` combat / acid / damage families

### Current proof level

- `Mods/QudJP/Assemblies/QudJP.Tests/L2/CombatAndLogMessageQueuePatchTests.cs:505-757`
  - proves both emit entrypoints can own and translate queued messages
- `Mods/QudJP/Assemblies/QudJP.Tests/L2G/TargetMethodResolutionTests.cs:257,465-469`
  - verifies the queue hook and both emit target signatures
- `docs/emit-message-coverage-audit.md:18-39,68-127`
  - explicitly describes emit as producer-owned but still only partially proven across its broad producer surface

### Verdict for EmitMessage

`EmitMessage` is a **producer-owned batch**, not a sink-observed one.

The next review unit should stay close to `GameObjectEmitMessageTranslationPatch` and the emit-route patterns, not the generic message-log sink.

## Best first review split

The best next decomposition is:

1. **AddPlayerMessage / sink-observed families**
   - keep queue sink as observation-only
   - review explicit queue overlays separately
2. **EmitMessage / producer-owned families**
   - review direct emit-route coverage under `GameObjectEmitMessageTranslationPatch`
   - use `messages.ja.json` + emit-route tests as the current ownership boundary

## Best first concrete review unit

Start with **EmitMessage producer-owned families**.

Why:

1. ownership is already explicit in code
2. tests already prove route wiring end-to-end
3. the route is large enough (`214`) that better family decomposition here will unlock a lot of unresolved work quickly
