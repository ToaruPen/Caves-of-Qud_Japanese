# Producer-First Architecture Design

## Acceptance Criteria for This Branch

- [ ] `UITextSkin.SetText` is audit-primary, not translation-primary
- [ ] 3+ existing families migrated to contract-based rendering
- [ ] logic-required determination is per-contract, not per-string
- [ ] L1/L2G tests exist for contract units
- [ ] Player.log missing-key reports shift from "string listing" to "unknown contract / unknown owner"
- [ ] AuditRoute false positive rate is bounded: known fixed labels do NOT flood as unclaimed

## Contract Types (6)

| Contract | Meaning | Translation Strategy |
|----------|---------|---------------------|
| `Leaf` | Stable literal, compile-time constant | Exact-match dictionary lookup |
| `MarkupLeaf` | Stable literal with markup boundaries (e.g., `{{W|Strength}}`) | Markup-preserving exact-match lookup |
| `LeafList` | Ordered list of stable literals (e.g., skill prerequisite names) | Per-item Leaf lookup, preserve list structure |
| `Template` | Format string with named/indexed slots | Slot extraction → template rendering |
| `Builder` | Ordered slot composition (adj+base+clause+tag) | Decompose → reorder for JP → recompose |
| `MessageFrame` | SVO sentence with actor/verb/object/prep | Frame extraction → SOV rewrite |

`Audit` is not a contract but an operating mode: UITextSkin.SetText runs in audit mode,
detecting strings that reached the sink without any contract claiming them.

### Boundary criteria

- **Leaf vs MarkupLeaf**: MarkupLeaf is used when the dictionary key must include color tags
  or TMP spans (e.g., `{{W|Strength}}`), and the translated output must preserve those markup
  boundaries. If the key is plain ASCII without markup, use Leaf.
- **LeafList vs Leaf**: LeafList is used when a field contains multiple Leaf items in a
  structured list (e.g., comma-separated skill names). The renderer iterates and translates
  each item individually.
- **Template vs Builder**: Template has indexed/named slots filled by `string.Format` or
  equivalent. Builder has semantically ordered slots (adj, base, clause, tag) that require
  JP word-order recomposition. If slots need reordering for Japanese, use Builder.

### Removed from contract types

`PostProcess` (grammar/filter transforms) is not a contract type. It is an internal pipeline
stage within each renderer. For example, a Template renderer may apply JP particle rules as
a post-render step. This is an implementation detail, not a registry-level classification.

## Renderer Interface

Each contract type has a corresponding renderer. The renderer interface is minimal:

```csharp
internal interface IContractRenderer
{
    string Render(string source, RouteContract contract);
}
```

Concrete renderers:

| Contract Type | Renderer | Derives From |
|---------------|----------|-------------|
| `Leaf` | `LeafRenderer` | Wraps `Translator.Translate()` exact-match |
| `MarkupLeaf` | `MarkupLeafRenderer` | Markup-preserving `Translator.Translate()` |
| `LeafList` | `LeafListRenderer` | Iterates items, delegates to `LeafRenderer` |
| `Template` | `TemplateRenderer` | Successor of `UITextSkinTemplateTranslator` |
| `Builder` | `BuilderRenderer` | Successor of `GetDisplayNameRouteTranslator` |
| `MessageFrame` | `MessageFrameRenderer` | New: SVO→SOV rewrite engine |

`MessagePatternTranslator` continues to handle regex-based message log patterns.
It operates at the message sink level and is NOT replaced by MessageFrameRenderer.
MessageFrameRenderer handles structured `XDidY`/`XDidYToZ` frames upstream;
`MessagePatternTranslator` handles free-form messages that bypass the frame API.

## Route Domains

### Phase 1: Infrastructure + 3 proven domains

#### 1. Display Name Builder
- **Hook**: `GetDisplayNameEvent.ProcessFor`
- **Contract**: `Builder(Mark, Adj, Base, Clause, Tag)`
- **Current state**: Patched (GetDisplayNameProcessPatch), but sink-first — translates the composed result
- **Target state**: Intercept at builder stage, decompose into slots, translate slots, recompose in JP order
- **Existing evidence**: `docs/ilspy-analysis.md` §6 (DisplayName Assembly Order), `DescriptionBuilder` constants
- **Prerequisite**: L2G test confirming `DescriptionBuilder` slot extraction against real DLL before migration

#### 2. Long Description / Look
- **Hook**: `Description.GetLongDescription`, `Look.GenerateTooltipContent`
- **Contracts** (split into two):
  - `Description.FlavorBody` → **Leaf** (stable prose text from XML assets)
  - `Description.RulesLines` → **Template** (rules lines with numeric slots, equipment stats)
- **Current state**: Patched (DescriptionLongDescriptionPatch, LookTooltipContentPatch)
- **Target state**: Separate rules-line templates from flavor leaves at the producer boundary
- **Split rule**: TBD — requires investigation of `GetLongDescription` output structure.
  Must define: (a) delimiter or heuristic for flavor vs rules boundary,
  (b) behavior when boundary is ambiguous or absent.
  See "Open Questions" section below.
- **Why split**: Treating the entire description as one contract recreates the current tooltip problem where flavor and rules are conflated

#### 3. Screen-specific templates (already producer-first)
- **Patches**: CharacterStatusScreen, SkillsAndPowers, Factions, Statistic.GetHelpText
- **Contracts**: Formalize existing patches with field-level contract types
- **Example**: `SkillsAndPowersStatusScreenDetailsPatch` has:
  - `skillNameText` → **Leaf**
  - `requirementsText` → **Template**
  - `requiredSkillsText` → **LeafList**

### Phase 2: New domains

#### 4. Conversation Runtime
- **Hook**: `IConversationElement.GetDisplayText`
- **Contract**: `Template` (variable-replaced conversation text)
- **Current state**: Hook #11 is `observable` but conversation translation is partial
- **Target state**: Translate after variable replacement but before UI rendering
- **Open question**: Slot values within conversation text (e.g., `=subject.name=`) may themselves
  need translation. The renderer must handle nested slot ownership. See "Open Questions" section.

#### 5. Long Description rules-line refinement
- Extend `Description.RulesLines` contract with full slot definitions based on Phase 1 learnings

#### 6. Message Frame (highest impact, highest risk)
- **Hook**: `XDidY` / `XDidYToZ` (IComponent wrappers)
- **Contract**: `MessageFrame(Actor, Verb, Object, Prep, Extra, Mood)`
- **Current state**: Hooks #4/#5 are `unobserved / unpatched`
- **Target state**: Intercept SVO frame, rewrite to SOV, emit Japanese sentence
- **Why last**: Currently unpatched, highest risk. Branch should be stable before adding this.
- **Impact**: Eliminates the entire class of string-concatenation fragment entries in message dictionaries

## Audit Route (UITextSkin.SetText)

### Current behavior
- Primary translation sink: attempts exact-match dictionary lookup on every string
- Falls back to `ResolveObservabilityContext` (stack trace analysis) for route reclassification
- Logs `missing key` for untranslated strings

### Target behavior (no fallback)

- Strings claimed by an upstream `TranslationScope` → pass through (already translated)
- Unclaimed strings → **no translation attempt**. Log as unclaimed:
  `[QudJP] AuditSink: unclaimed '<text>' (owner: unknown, context: <stack-hint>)`
- Stack trace reclassification (`ResolveObservabilityContext`) removed entirely
- All translation happens upstream at the producer boundary, never at the sink

**No fallback at the sink.** Fixed labels (UI text like "Inventory", "SKILLS") must be
claimed by an upstream contract (Leaf or MarkupLeaf) registered in ContractRegistry.
If a fixed label appears as unclaimed in the audit log, the correct response is to register
it in ContractRegistry with an upstream route — not to add a sink-level fallback.

This is intentional: fallback at the sink is what created the current anti-pattern of
exact-key dictionary growth for dynamically composed text. Removing it forces all
translation to be owner-attributed.

### Blind spot and noise policy

Known sources of audit noise that should NOT trigger "unclaimed" investigation:

- `HistoricStringExpander` output: procedurally generated names/lore. Intentionally
  disabled (`docs/procedural-text-status.md`). These strings are expected to appear
  untranslated. Audit log should tag them as `(blind-spot: procedural)`.
- Empty strings, whitespace-only strings, single-character strings: noise. Suppress.
- Strings that are purely numeric (e.g., `"12"`, `"3/5"`): UI data values, not translatable.
  Suppress.
- Version strings (e.g., `"1.0.4"`): not game text. Suppress.

These suppression rules are implemented in `UITextSkinTranslationPatch` audit mode
and documented as a known-exclusion list.

### Patch priority

`UITextSkinTranslationPatch` (audit-only) must run AFTER all upstream patches.
Use `[HarmonyPriority(Priority.Last)]` to ensure it is the final Postfix on `SetText`.

### Migration path
1. Add `TranslationScope` (thread-static **stack**) that upstream patches push/pop
2. Register all existing Leaf/MarkupLeaf entries in ContractRegistry with upstream routes
3. Formalize existing screen patches with TranslationScope push/pop (simultaneous with step 2)
4. UITextSkin switches to audit-only mode + remove `ResolveObservabilityContext` (single cutover)

## TranslationScope

Thread-static **stack** implementation to handle nested renders and re-entry safely.

```csharp
internal static class TranslationScope
{
    [ThreadStatic] private static Stack<ScopeFrame>? _stack;

    internal static void Push(string routeId, string schemaId, string? fieldId = null)
    {
        // ThreadStatic fields are null on first access per thread — lazy init required
        _stack ??= new Stack<ScopeFrame>();
        _stack.Push(new ScopeFrame(routeId, schemaId, fieldId));
    }

    internal static void Pop()
    {
        if (_stack is { Count: > 0 })
            _stack.Pop();
    }

    internal static ScopeFrame? Peek()
    {
        return _stack is { Count: > 0 } ? _stack.Peek() : null;
    }

    internal static bool IsActive => _stack is { Count: > 0 };
}

internal readonly record struct ScopeFrame(string RouteId, string SchemaId, string? FieldId);
```

### Scope lifecycle: Push in Prefix, Pop in Finalizer

**Critical**: Pop must happen in a `[HarmonyFinalizer]`, not a `[HarmonyPostfix]`.
If the patched method throws an exception, Postfix is skipped but Finalizer always runs.
This prevents scope stack corruption on error paths.

```csharp
[HarmonyPrefix]
static void Prefix() => TranslationScope.Push("MyRoute", "MySchema");

[HarmonyFinalizer]
static void Finalizer() => TranslationScope.Pop();
```

Why a stack:
- Nested renders (e.g., a Builder that internally calls a Leaf lookup for a slot value)
- Re-entrant Harmony patches (Postfix of one patch triggers Prefix of another)
- Child renders must not corrupt parent scope

## Contract Registry

The registry is the source of truth for "what text exists and how it should be translated."

### Initialization order

ContractRegistry must be populated BEFORE `UITextSkin.SetText` audit mode activates.
Each `[HarmonyPatch]` class registers its contracts in its static constructor.
This guarantees registration happens at patch application time, before any game method is called.

### Hot-reload safety

`Register()` uses `Overwrite` semantics for duplicate keys (`RouteId + SchemaId + FieldId`).
This prevents double-registration errors when the mod is disabled and re-enabled without
restarting the game (Unity AppDomain persists).

### Schema (per field, not per route)

```
RouteId              → which Harmony route adapter owns this
SchemaId             → which contract schema applies (e.g., "DisplayName.AdjBaseClauseTag")
FieldId              → specific field within the route (e.g., "skillNameText", "requirementsText")
ContractType         → Leaf | MarkupLeaf | LeafList | Template | Builder | MessageFrame
Slots                → ordered list of semantic slot names (empty for Leaf)
Renderer             → IContractRenderer instance for this contract
ObservabilityFamily  → family label for DynamicTextProbe logging
```

### Implementation: `ContractRegistry.cs`

A static registry populated at mod init time. Each upstream route adapter registers its contracts.

```csharp
// Single-field route
ContractRegistry.Register(new RouteContract {
    RouteId = "StatisticGetHelpTextPatch",
    SchemaId = "Statistic.HelpText",
    FieldId = null,
    ContractType = ContractType.MarkupLeaf,
    Slots = Array.Empty<string>(),
    ObservabilityFamily = "Statistic.HelpText",
});

// Hybrid route with multiple field contracts
ContractRegistry.Register(new RouteContract {
    RouteId = "SkillsAndPowersStatusScreenDetailsPatch",
    SchemaId = "SkillsDetails.SkillName",
    FieldId = "skillNameText",
    ContractType = ContractType.Leaf,
    Slots = Array.Empty<string>(),
    ObservabilityFamily = "SkillsDetails.SkillName",
});

ContractRegistry.Register(new RouteContract {
    RouteId = "SkillsAndPowersStatusScreenDetailsPatch",
    SchemaId = "SkillsDetails.Requirements",
    FieldId = "requirementsText",
    ContractType = ContractType.Template,
    Slots = new[] { "skillName", "level" },
    ObservabilityFamily = "SkillsDetails.Requirements",
});
```

### Runtime flow

```
upstream patch fires
  → push TranslationScope(routeId, schemaId, fieldId)    [Prefix]
  → extract slots from game data per ContractType
  → call renderer for this contract type
  → emit translated Japanese string
  → pop TranslationScope                                  [Finalizer]

UITextSkin.SetText fires                                   [Priority.Last]
  → check TranslationScope.IsActive
  → if active: pass through (already translated by upstream)
  → if NOT active:
      → check blind-spot/noise suppression list
      → if suppressed: pass through silently
      → otherwise: log as unclaimed (no translation attempt)
```

## Screen-Specific Patches: Keep as Producer-First

These stay because they ARE producer-first — they intercept at a specific upstream method:

- `CharacterStatusScreenTranslationPatch` → `UpdateViewFromData` (Template: attributePoints, mutationPoints)
- `SkillsAndPowersStatusScreenTranslationPatch` → `UpdateViewFromData` (Template: spText)
- `SkillsAndPowersStatusScreenDetailsPatch` → `UpdateDetailsFromNode` (hybrid: skillName=Leaf, requirements=Template, requiredSkills=LeafList)
- `FactionsStatusScreenTranslationPatch` → `UpdateViewFromData` (Template: reputation lines)
- `StatisticGetHelpTextPatch` → `Statistic.GetHelpText` (MarkupLeaf)
- `CharacterStatusScreenMutationDetailsPatch` → mutation detail (Template: description + rank text)
- `CharacterStatusScreenAttributeHighlightPatch` → attribute highlight (MarkupLeaf)

These are already producer-first in spirit. The migration formalizes them with contract types
and registers them in ContractRegistry so they push TranslationScope.

## What Gets Removed/Replaced

| Current | Replacement |
|---------|-------------|
| `UITextSkinTranslationPatch` as primary translator | Audit-only mode |
| `ResolveObservabilityContext` (stack trace reclassification) | `TranslationScope` push/pop stack |
| Per-string `missing key` → exact-key dictionary growth | Per-contract `unclaimed` → upstream investigation |
| `Translator.Translate()` as the universal entry point | Contract-specific renderers; `Translator` limited to Leaf/MarkupLeaf via `LeafRenderer` |
| Fragment dictionary entries (`"You stagger "`, `" with your shield block!"`) | MessageFrame contract (Phase 2) |

## Test Strategy

### L1: Contract logic tests
- Builder slot decomposition and JP recomposition
- Template slot extraction and rendering
- MarkupLeaf markup preservation across translation
- LeafList iteration and per-item rendering
- TranslationScope push/pop/peek stack behavior (including exception-path pop via Finalizer)
- ContractRegistry registration, lookup, and overwrite semantics

### L2: Harmony integration tests
- Upstream patch pushes TranslationScope
- Contract renderer produces correct Japanese
- UITextSkin audit mode detects unclaimed strings (no translation attempt)
- UITextSkin passes through claimed strings without re-translation
- Blind-spot suppression: procedural names, numeric values, empty strings are not logged

### L2G: DLL-assisted tests
- Route target methods resolve against real Assembly-CSharp.dll
- Contract slot structures match actual game method signatures
- DescriptionBuilder slot extraction validated before DisplayName Builder migration
- No regression on existing translations

## Implementation Order

### Phase 1: Infrastructure + proven domains
1. `TranslationScope` (thread-static stack, Finalizer pop) + `ContractRegistry` (overwrite semantics, static ctor init) + `ContractType` enum + `IContractRenderer` interface + concrete renderers
2. Register existing Leaf/MarkupLeaf entries + formalize existing screen patches with TranslationScope push/pop (simultaneous)
3. UITextSkin audit-only mode + remove `ResolveObservabilityContext` (single cutover)
   - **Cutover checklist**: simultaneously cut over `DescriptionLongDescriptionPatch` and
     `GetDisplayNameProcessPatch` which call `TranslatePreservingColors()` directly —
     these do not go through SetText but depend on the helper that may be refactored
4. Display Name Builder contract migration (L2G prerequisite: DescriptionBuilder slot validation)
5. Long Description split: intercept contributing producers (NOT output split)
   - **Prerequisite**: L2G test confirming `GetShortDescription()` signature against real DLL.
     Add to `ilspy-analysis.md` hook table as supplementary entry.

### Phase 2: New domains
6. Conversation runtime Template migration
7. Message Frame contract (XDidY/XDidYToZ — new patch, highest risk)
8. Cleanup: remove fragment dictionary entries made obsolete by contracts

### Post-Phase 1 documentation tasks
9. Update `Mods/QudJP/Assemblies/AGENTS.md` with ContractRegistry registration pattern
10. Update `AGENTS.md` (root) to reference `producer-first-design.md` instead of deleted docs
11. Update `README.md` to remove references to deleted `translation-process.md`

## Implementation Checklist (from cross-review)

These items must be addressed during implementation, not as separate tasks:

- [ ] **Before Phase 1 Step 5**: Confirm `GetShortDescription()` signature via L2G test; add to `ilspy-analysis.md` hook table
- [ ] **During Phase 1 Step 3 (cutover)**: Simultaneously update `DescriptionLongDescriptionPatch` and `GetDisplayNameProcessPatch` to not depend on `TranslatePreservingColors()` being called at sink level
- [ ] **During Phase 1 Step 2 (OQ-3 refactor)**: After changing `TranslateStringField` signature, update `MainMenuLocalizationPatchTests.cs` and `OptionsLocalizationPatchTests.cs` (existing tests must not be deleted — update them)
- [ ] **After Phase 1 completion**: Add ContractRegistry registration pattern documentation to `Mods/QudJP/Assemblies/AGENTS.md`
- [ ] **Non-SetText patches**: Document explicitly that patches which rewrite `__result` directly (e.g., `GetDisplayNameProcessPatch`) do NOT need TranslationScope because they never reach the UITextSkin audit sink. Add a "Scope Exemption" note in the design doc or AGENTS.md for implementors.

## Investigation Results (formerly Open Questions)

### OQ-1: Long Description split boundary — RESOLVED

**Finding: Output-level split is not possible.**

`GetLongDescription(StringBuilder)` mixes flavor and rules in a single StringBuilder.
The `\n\n` delimiter appears both between sections AND within flavor text itself
(e.g., Mark/Weight appended with `\n\n` inside `GetShortDescription`).

Build sequence:
1. `SB.Append(Short)` — flavor body (from `GetShortDescription()`, may contain `\n\n`)
2. `"\n\nGender: "` + value (if applicable)
3. `"\n\nPhysical features: "` + comma-separated list
4. `"\nEquipped: "` + comma-separated list
5. `"\n\n"` + effects block (from `GetEffectsBlock` event)

**Decision**: Do NOT split at the output. Instead, split at the **producer boundary**:
- FlavorBody: Patch `GetShortDescription()` separately (returns flavor prose only)
- RulesLines: Patch the Body/Effects append calls individually via Transpiler or
  by intercepting the contributing events (`GetEffectsBlock`, equipment rendering)

This changes Phase 1 Step 5 from "split output" to "intercept contributing producers."

### OQ-2: Conversation slot ownership — RESOLVED

**Finding: `GameText.VariableReplace` handles all `=token=` expansion during `Prepare()` stage.**

Pipeline: `Prepare()` → `PrepareTextEvent.Send` → `GameText.VariableReplace` → `PrepareTextLateEvent.Send`

After `Prepare()`, `IConversationElement.Text` contains fully assembled text with no remaining tokens.

**Optimal hook points:**
- Node body: `IConversationElement.Prepare()` Postfix — text is complete, before UI render
- Choice labels: `IConversationElement.GetDisplayText()` Postfix — after DisplayTextEvent

Slot values (e.g., NPC names expanded from `=subject.name=`) are already resolved by the time
our hook fires. The conversation renderer does NOT need to handle slot translation — it receives
complete sentences with all variables expanded.

### OQ-3: TranslateStringField helper migration — RESOLVED

**Finding: 9 call sites across 3 production patches. Refactor, don't remove.**

| Patch | Call sites | Fields |
|-------|-----------|--------|
| `PickGameObjectScreenTranslationPatch` | 4 | Description (collection + single) |
| `MainMenuLocalizationPatch` | 2 | Text (LeftOptions, RightOptions) |
| `OptionsLocalizationPatch` | 3 | Title, HelpText, Description |

**Decision**: Refactor helpers to accept `RouteContract` parameter and push `TranslationScope`
internally. This preserves the reflection + null-safety abstraction while adding contract
awareness. Each calling patch registers its contracts and passes the contract to the helper.

```csharp
// New signature
internal static void TranslateStringField(
    object? instance, string fieldName, RouteContract contract)
```

This is lower risk than removing helpers (9 call sites expand ~5-8x each if inlined).

### OQ-4: Leaf entry inventory — RESOLVED

**Finding: ~5,647 Leaf entries (82% of 6,813 total). Initial cutover: ~600-700 entries.**

| Dictionary | Leaf entries | Confidence |
|-----------|-------------|------------|
| `ui-default.ja.json` | ~207 | 97% fixed labels |
| `ui-displayname-adjectives.ja.json` | ~50 | 95% |
| `ui-liquid-adjectives.ja.json` | ~25 | 98% |
| `ui-liquids.ja.json` | ~40 | 100% |
| `ui-attributes.ja.json` | ~40 | 90% |
| `ui-skillsandpowers.ja.json` | ~35 | 85% |
| Other UI dictionaries | ~200 | varies |
| **Initial cutover total** | **~600-700** | |

Remaining ~4,900 entries migrate in Phase 2+ as specialized routes claim ownership.

**Registration approach**: Each dictionary file maps to one or more upstream routes.
`ui-default.ja.json` entries are primarily owned by `UITextSkinTranslationPatch` (to be
formalized as explicit Leaf contracts). Display-name entries by `GetDisplayNameRouteTranslator`.
