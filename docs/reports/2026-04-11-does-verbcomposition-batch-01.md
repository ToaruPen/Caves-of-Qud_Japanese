# 2026-04-11 Does / VerbComposition batch 01

## Scope

This note starts the `source_route=Does`, `type=VerbComposition`, `status=needs_review` bucket from `docs/candidate-inventory.json`.

Current inventory size: **283** sites.

## Existing owner seam already present

The repo already contains a dedicated Does seam:

- `Mods/QudJP/Assemblies/src/Patches/DoesFragmentMarkingPatch.cs:8-60`
  - patches `XRL.World.GameObject:Does(...)`
  - marks the resolved verb fragment in the returned string
- `Mods/QudJP/Assemblies/src/Patches/DoesVerbRouteTranslator.cs:14-56,77-146,176-230`
  - parses the marked/plain sentence
  - normalizes subject + verb + extra
  - reuses `MessageFrameTranslator.TryTranslateXDidY(...)`

So this bucket is **not** starting from zero architecture.

## Existing tests already in repo

- `Mods/QudJP/Assemblies/QudJP.Tests/L1/DoesVerbRouteTranslatorTests.cs:49-81`
  - verifies marked and plain Does-route translation through the current seam
- `Mods/QudJP/Assemblies/QudJP.Tests/L1/DoesVerbFamilyTests.cs:76-257`
  - already covers major Does family groups such as:
    - status predicate
    - negation/lack
    - combat damage
    - possession/lack
    - motion/direction
    - extended status/state
    - social/persuasion

This means the bucket already has both seam-level and family-level test scaffolding.

## Inventory / manifest split

The best current decomposition is already captured in `docs/superpowers/plans/2026-03-24-does-verb-manifest.md:7-20`:

- `message-frame-normalizable`: **78**
- `does-composition-specific`: **66**
- `emit-message-overlap`: **3**
- `needs-harmony-patch`: **10**

This is more useful than treating all 283 rows as one undifferentiated bucket.

## Top file concentrations from current inventory

Largest current files in the bucket:

| Count | File |
| ---: | --- |
| 26 | `XRL.World.Parts/MissileWeapon.cs` |
| 8 | `XRL.UI/TradeUI.cs` |
| 8 | `XRL.World.Parts/ITeleporter.cs` |
| 7 | `XRL.World.Parts/Combat.cs` |
| 6 | `XRL.World.Parts.Skill/Tactics_Kickback.cs` |
| 6 | `XRL.World.Parts/Campfire.cs` |

These are useful review units, but the manifest split is the stronger first cut.

## Current reading of the bucket

### 1. Message-frame-normalizable rows

Examples from the manifest:

- `stunned` (`Stun.cs`) — `docs/superpowers/plans/2026-03-24-does-verb-manifest.md:24-29,38-40`
- `open` (`ExtradimensionalHunterSummoner.cs`) — `27,73`
- `already full` / `already fully loaded` (`MagazineAmmoLoader.cs`) — `75-77`
- `falls/returns to the ground` family — `125`

These rows look like the fastest progress path because they can stay on the existing Does seam while reusing `verbs.ja.json` / `MessageFrameTranslator` behavior.

### 2. does-composition-specific rows

Examples:

- `is empty` / `is unresponsive` — `50-55`
- `has no room for more {x}` — `56-58`
- `encoded with ...` (`ITeleporter`) — `98-100`
- long social / busy / ownership clauses — `101-103`, `179-190`

These look like true family translators/helpers at the producer seam, not simple message-frame reuse.

### 3. emit-message-overlap rows

- `306`, `436`, `437` are explicitly called out as better treated under `EmitMessage` provenance, not true Does ownership (`docs/superpowers/plans/2026-03-24-does-verb-manifest.md:18,111,181-182,196-197`).

### 4. needs-harmony-patch leftovers

- `exhausted`, `sealed`, `not bleeding`, `no limbs`, `beeps loudly and flashes a warning glyph` — `35-46`, `74`, `107`, `158`

These are the genuine leftovers after normalizable / family-specific rows are removed.

## Best first concrete review unit

The best first unit is **message-frame-normalizable quick wins**.

Why:

1. They already fit the current Does seam.
2. They likely need asset/tier alignment more than new architecture.
3. The manifest already names concrete examples (`stunned`, `open`, `starts to glitch`) as the first recommended implementation order (`14-17, 24-29`).

## Initial verdict

The `Does / VerbComposition` bucket is **not blocked on missing architecture**.

It already has:

- a route seam (`DoesFragmentMarkingPatch` + `DoesVerbRouteTranslator`)
- seam tests
- family tests
- a prior manifest that splits the bucket into actionable subgroups

The next autonomous pass should start with:

1. `message-frame-normalizable` quick wins
2. then `does-composition-specific` family translators
3. keeping `emit-message-overlap` out of the true Does queue
