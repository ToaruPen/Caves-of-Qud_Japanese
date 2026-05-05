# CodeRabbit Owner Route Coverage Matrix

This CodeRabbit guideline is a derived, raw-free review matrix for QudJP runtime text
ownership. It complements the Roslyn inventory:

- `docs/coderabbit/roslyn-text-construction-inventory-summary.md`

The Roslyn inventory answers "where does the game construct text?". This matrix
answers "where may QudJP own translation, and where should it only observe?".

Do not copy decompiled source, raw game strings, method bodies, or large symbol
lists into review comments. Ask for tests, route notes, or fresh runtime
evidence instead.

## Ownership Vocabulary

| Term | Review meaning |
| --- | --- |
| `producer-owned` | QudJP translates at the route that builds the stable source text or a stable template. |
| `mid-pipeline-owned` | QudJP translates after the game has assembled enough route-specific structure but before the final UI sink. |
| `asset-owned` | QudJP owns the string through a shipped XML/JSON localization asset because the leaf is stable. |
| `observation-only` | QudJP may log final/source evidence but should not mutate visible text at that location. |
| `mixed` | Some overloads or call paths are owned, while generic fallbacks remain observation-only. |

CodeRabbit should prefer owner routes over final UI sinks. A visible English
string disappearing at a sink is not enough; the PR should prove the owner route,
stable source shape, dynamic fragments, markup contract, and tests.

## Route Matrix

| Route family | Default treatment | Evidence anchors | CodeRabbit should flag |
| --- | --- | --- | --- |
| `UITextSkin.SetText` and direct UI text assignment | `observation-only` unless a screen-specific owner contract exists | `Mods/QudJP/Assemblies/QudJP.Tests/L2/UITextSkinTranslationPatchTests.cs`, `Mods/QudJP/Assemblies/QudJP.Tests/L2/UITextSkinTemplateTranslatorTests.cs`, Roslyn surfaces `SetText` and `DirectTextAssignment` | Sink-wide translation, dictionary entries hiding procedural text, missing screen/field ownership tests, dropped direct marker handling |
| Generic message log / `AddPlayerMessage` | `observation-only` by default; producer handoff paths may be owned | `Mods/QudJP/Assemblies/QudJP.Tests/L1/MessageLogPatchTests.cs`, `Mods/QudJP/Assemblies/QudJP.Tests/L2/MessageLogPatchTests.cs`, `Mods/QudJP/Assemblies/QudJP.Tests/L1/MessageLogProducerTranslationHelpersTests.cs`, Roslyn surface `AddPlayerMessage` | Treating every log message as one flat dictionary route, bypassing producer helpers, failing to preserve direct markers and color markup |
| Popup final display | `mixed`; route-specific popup producers can be owned, generic fallback remains cautious | `Mods/QudJP/Assemblies/QudJP.Tests/L2/PopupTranslationPatchTests.cs`, `PopupShowTranslationPatchTests.cs`, `PopupMessageTranslationPatchTests.cs`, `PopupRouteHandoffTranslationTests.cs`, `PopupAskStringTranslationPatchTests.cs`, `PopupAskNumberTranslationPatchTests.cs`, Roslyn surfaces `Popup` and `TutorialManagerPopup` | Broad popup fallback without route tests, translating prompt/input control text as content, missing overload/target verification |
| `XDidY` and message-frame composition | `producer-owned` or `mid-pipeline-owned` | `Mods/QudJP/Assemblies/QudJP.Tests/L1/MessageFrameTranslatorTests.cs`, `Mods/QudJP/Assemblies/QudJP.Tests/L1/Pbt/MessageFrameTranslatorPropertyTests.cs`, Roslyn surface `MessageFrame` | SVO English structure leaking into Japanese, color/placeholder loss across fragments, translating after the frame is flattened |
| `Does` verb-family text | Route-owned translator; do not patch `Does()` directly without a route contract | `Mods/QudJP/Assemblies/QudJP.Tests/L1/DoesVerbRouteTranslatorTests.cs`, `DoesVerbFamilyTests.cs`, Roslyn surface `Does` | Direct broad `Does()` mutation, verb token leakage, Japanese verb/preposition order not asserted |
| Object descriptions and inspect/status descriptions | Route-owned when short/long/effect owner tests exist; otherwise asset-owned only for fixed leaves | `Mods/QudJP/Assemblies/QudJP.Tests/L1/DescriptionTextTranslatorTests.cs`, `Mods/QudJP/Assemblies/QudJP.Tests/L2/DescriptionShortDescriptionPatchTests.cs`, `DescriptionLongDescriptionPatchTests.cs`, `DescriptionInspectStatusPatchTests.cs`, Roslyn surfaces `DescriptionAssignment`, `DescriptionReturn` | Converting procedural descriptions into flat dictionary leaves, missing short/long route distinction, untested markup restoration |
| Active effects and effect descriptions | Route-owned where effect translator/owner tests exist | `Mods/QudJP/Assemblies/QudJP.Tests/L1/ActiveEffectTextTranslatorTests.cs`, `Mods/QudJP/Assemblies/QudJP.Tests/L2/ActiveEffectsOwnerPatchTests.cs`, Roslyn surface `EffectDescriptionReturn` | Translating only final effect list UI, losing duration/count fragments, missing fallback for unsupported effect text |
| Display names and `GetDisplayName` processing | Route-owned only when the event/process contract is tested; unresolved display buckets are observation-only | `Mods/QudJP/Assemblies/QudJP.Tests/L1/GetDisplayNameRouteTranslatorTests.cs`, `GetDisplayNameProcessPatchLogicTests.cs`, `Mods/QudJP/Assemblies/QudJP.Tests/L2/GetDisplayNameProcessPatchTests.cs`, `ZoneDisplayNameTranslationPatchTests.cs`, Roslyn surfaces `DisplayNameAssignment`, `DisplayNameReturn`, `GetDisplayName` | Broad display-name dictionary catchalls, stripping mutation markers/color codes, translating renderer-only labels without owner proof |
| Activated ability and character generation UI text | Owner depends on the exact API/screen route | `Mods/QudJP/Assemblies/QudJP.Tests/L2/ChargenAttributeDescriptionTranslationPatchTests.cs`, Roslyn surface `ActivatedAbility` | Asset-only fixes for dynamic ability text, missing UI route tests, unverified overload targets |
| Journals, history spice, and annals-style generated prose | Route-family clue; require a specific owner or documented observation-only decision | Roslyn surfaces `JournalAPI`, `HistoricStringExpander` | Claims of complete generated prose coverage without inventory update, tests, or runtime evidence |
| Renderer, inventory, equipment, screen hierarchy, and final-output probes | `observation-only` unless a field-specific owner test exists | `Mods/QudJP/Assemblies/QudJP.Tests/L1/FinalOutputObservabilityTests.cs`, `InventoryRenderObservabilityTests.cs`, `InventoryUiFieldObservabilityTests.cs`, `EquipmentLineObservabilityTests.cs`, `ScreenHierarchyObservabilityTests.cs` | Mutating renderer output as a generic fix, treating observability as coverage proof, unstable probe schema changes |
| XML/JSON localization assets | `asset-owned` for stable fixed leaves only | `Mods/QudJP/Localization/AGENTS.md`, `scripts/check_translation_tokens.py`, `scripts/check_glossary_consistency.py` | Runtime variables in fixed-leaf dictionaries, source-key normalization, duplicate divergent translations, token/glossary gate bypasses |

## Review Rules

When a PR touches C# translation behavior, CodeRabbit should check:

1. The changed route has a named owner category from this matrix.
2. Roslyn inventory surfaces for the route were consulted or updated when the
   PR claims broader runtime coverage.
3. Tests exist at the correct layer:
   - L1 for pure translator logic;
   - L2 for Harmony behavior against dummy targets;
   - L2G for real game target/signature resolution;
   - L3 logs only for runtime smoke evidence.
4. Sink-adjacent routes remain observation-only unless the PR proves a
   route-specific owner with tests.
5. Direct translation markers, Qud markup, TMP color tags, placeholders, and
   dynamic fragments are preserved according to the route contract.

If a PR claims "complete text coverage" or "all English fixed", require updates
to both the Roslyn summary and this ownership matrix, plus local regeneration
evidence for the full inventory, or a precise scope statement that limits the
claim.
