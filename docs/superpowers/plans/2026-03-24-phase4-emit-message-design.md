# Phase 4: emit-message Producer Migration Design

Date: 2026-03-24

## Scope

116 patterns in `messages.ja.json` with `route=emit-message`. These match text emitted through `AddPlayerMessage` (intercepted by `MessageLogPatch`).

## Key Insight

Unlike Phase 3 (does-verb → verbs.ja.json), these patterns **cannot simply move to a different data file**. They need producer-side Harmony patches that translate text BEFORE it reaches `AddPlayerMessage`.

However, creating 30+ new Harmony patches is impractical in one PR. The practical approach is:

1. **Patterns that already have producer coverage** → remove from messages.ja.json (already translated by producer)
2. **Leaf-like patterns (no captures)** → move to leaf dictionary
3. **Patterns whose producers are already patched** → extend existing patches
4. **Remaining patterns** → keep in messages.ja.json with route='emit-message' until dedicated producers are built in later phases

## Pattern Breakdown

| Category | Count | Strategy |
|----------|-------|----------|
| Combat hit/crit/miss/damage | 37 | Keep — needs MissileWeapon/Combat producer patches (Phase 6) |
| Journal accomplishments (slew, discovered, traveled...) | 20 | Move to journal-patterns.ja.json — JournalTextTranslator already owns these |
| Inventory actions (pick up, drop, equip, fire, reload) | 7 | Keep — common UI actions, low risk in sink |
| Perception (see, hear, feel) | 6 | Keep — diverse producers |
| Door/lock interactions | 8 | Keep — needs Door.cs producer patch |
| Harvest | 5 | Keep — partially covered by verbs.ja.json |
| Movement/navigation | 7 | Keep — diverse producers |
| Bleeding/nosebleed | 4 | Keep — needs Nosebleed.cs producer patch |
| Game stats (XP, HP) | 3 | Keep — core UI feedback |
| No-capture leaf patterns | 6 | Move to leaf dictionary |
| Misc (ammo, tinkering, rind, genome, infiltrate) | 13 | Keep — scattered producers |

## Actionable Now (Phase 4a)

### 1. Move 6 leaf-like patterns → ui-messagelog-leaf.ja.json
Patterns with no regex captures that are effectively exact strings.

### 2. Move ~20 journal accomplishment patterns → journal-patterns.ja.json
Patterns like "You slew X.", "You discovered X.", "You traveled to X." are journal entries. JournalTextTranslator already uses JournalPatternTranslator, so these belong there.

### 3. Reclassify remaining 90 patterns as 'emit-message-deferred'
These stay in messages.ja.json but with an updated route tag indicating they need future producer work.

## Phase 4a Implementation

- Move leaf patterns (6) → exact dictionary entries
- Move journal patterns (~20) → journal-patterns.ja.json
- Update route tags for remaining patterns
- No new C# code needed
- All existing tests should continue passing

## Future Phases (not in Phase 4)

### Phase 4b: Combat producer patches
- MissileWeapon hit/crit/damage family (22 patterns)
- Combat.cs melee hit/miss/block/dodge/parry (15 patterns)

### Phase 4c: Interaction producer patches
- Door.cs lock/unlock/open/close (8 patterns)
- Nosebleed.cs bleeding (4 patterns)

### Phase 4d: Remaining scattered producers
- Inventory actions, perception, movement, misc (45 patterns)

## Risk Assessment

Moving patterns to journal or leaf is low-risk (same translation mechanism, different file).
Reclassifying routes is zero-risk (route tag is metadata only, not used at runtime).
No C# changes in Phase 4a means no Harmony patch risks.
