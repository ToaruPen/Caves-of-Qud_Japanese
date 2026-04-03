# 2026-04-03 color-tag route audit

## Scope

- **Task:** comprehensive audit of color-tag handling for issue #308 context on branch `codex/issue-308-color-tags`
- **Evidence hierarchy:** tests under `Mods/QudJP/Assemblies/QudJP.Tests/` are the behavior source of truth; runtime logs are evidence only
- **Non-goals:** no feature changes, fixes, sync, commit, push, or PR work

## Current worktree context

The current branch delta is narrowly scoped to the AbilityBar active-effects route:

- `Mods/QudJP/Assemblies/src/Patches/AbilityBarAfterRenderTranslationPatch.cs`
- `Mods/QudJP/Assemblies/QudJP.Tests/L2/AbilityBarAfterRenderTranslationPatchTests.cs`

The diff adds a TMP-colored active-effects case and changes separator/effect-slice restoration so the route can preserve TMP `<color>` spans without reintroducing Qud markup.

## Executive answer

**How far the repository can currently handle color tags correctly:** QudJP can strongly support **producer-owned, test-backed color preservation**. At that boundary it can strip, translate, and restore Qud `{{...}}`, foreground `&x`, background `^x`, and TMP `<color=...>` tokens through shared helpers, and it has route-level proof for major producer families such as **GetDisplayName**, **ZoneDisplayName**, and **popup producer handoff**.

**Where that ability stops:** support stops **before sink-side rendering**. `UITextSkinTranslationPatch` is intentionally observation-only for unclaimed sink text, and the final game-side render chain (`UITextSkin.SetText -> Apply -> ToRTFCached -> RTF.FormatToRTF -> Sidebar.FormatToRTF`) is outside QudJP's ownership. Mixed TMP/legacy-markup routes still need runtime confidence, especially **AbilityBar active effects** and **popup options with embedded `{{hotkey|...}}` markup**. Modern UI conversion also drops `^` background color codes in `XRL.UI.Sidebar.FormatToRTF`.

## Shared color pipeline contract

### 1. Shared helper guarantees

`Mods/QudJP/Assemblies/src/ColorCodePreserver.cs` and `Mods/QudJP/Assemblies/src/ColorAwareTranslationComposer.cs` define the repository's reusable color contract.

- `ColorCodePreserver.Strip(...)` / `Restore(...)` handle:
  - Qud wrappers like `{{W|text}}`
  - foreground codes like `&G`
  - background codes like `^r`
  - TMP tags like `<color=#44ff88>text</color>`
- `ColorAwareTranslationComposer` is the only approved restore owner; `QudJP.Tests/L1/ColorRestoreOwnershipTests.cs` enforces that no other source file calls `ColorCodePreserver.Restore(...)` directly.
- `QudJP.Tests/L1/ColorCodePreserverTests.cs` and `QudJP.Tests/L1/ColorMarkupTokenizerTests.cs` prove helper-layer preservation for all token forms above.

**Strongly supported at this layer:** token preservation itself.

### 2. Sink/render boundary

The game-side renderer boundary is upstream of QudJP's guarantees:

- `XRL.UI.UITextSkin.SetText(string)` only assigns `text`, clears cache, and calls `Apply()` (`/Users/sankenbisha/Dev/coq-decompiled/XRL.UI/UITextSkin.cs:116-125`)
- `Apply()` converts via `text.ToRTFCached(...)` (`/Users/sankenbisha/Dev/coq-decompiled/XRL.UI/UITextSkin.cs:168-176`)
- `ToRTFCached()` delegates to `Qud.UI.RTF.FormatToRTF(...)` (`/Users/sankenbisha/Dev/coq-decompiled/Extensions.cs:592-599`, `/Users/sankenbisha/Dev/coq-decompiled/Qud.UI/RTF.cs:9-23`)
- `XRL.UI.Sidebar.FormatToRTF(...)` transforms legacy markup to TMP `<color>` and **drops `^` background formatting instead of preserving it** (`/Users/sankenbisha/Dev/coq-decompiled/XRL.UI/Sidebar.cs:650-716`)

**Implication:** strong support must be defined as **producer-owned translation plus helper preservation**, not as final render correctness.

## Route catalog

Guarantee levels used below:

- **Strongly supported** — helper + route tests prove the route's ownership and behavior
- **Source-traceable** — source ownership is clear, but this audit did not find enough route-specific proof to claim stronger guarantees
- **Runtime-required** — source/tests exist, but current runtime evidence is still needed before claiming correctness
- **Unsupported/stop point** — outside repository ownership or explicitly observation-only

| Route | Producer / owner | Typical input shape | Transformation boundary | Sink / consumer | Evidence | Guarantee |
|---|---|---|---|---|---|---|
| Shared helper layer | `ColorCodePreserver` + `ColorAwareTranslationComposer` | `{{...}}`, `&x`, `^x`, `<color=...>` | Strip -> translate visible text -> restore spans | Any producer route that uses helpers | `src/ColorCodePreserver.cs`, `src/ColorAwareTranslationComposer.cs`, `QudJP.Tests/L1/ColorCodePreserverTests.cs`, `QudJP.Tests/L1/ColorMarkupTokenizerTests.cs`, `QudJP.Tests/L1/ColorRestoreOwnershipTests.cs` | **Strongly supported** |
| Display name: `GetDisplayNamePatch` | `XRL.World.GetDisplayNameEvent.ProcessFor(...)` returns `Markup.Wrap(DB.ToString())` | legacy Qud markup around composed display names and suffixes | `GetDisplayNameRouteTranslator.TranslatePreservingColors(...)` in `GetDisplayNamePatch` | whichever UI/message route consumes `DisplayName` | `src/Patches/GetDisplayNamePatch.cs`, `src/Patches/GetDisplayNameRouteTranslator.cs`, `/Users/sankenbisha/Dev/coq-decompiled/XRL.World/GetDisplayNameEvent.cs:275-336`, runtime probes at `Player.log:754,780` | **Strongly supported** |
| Display name: `GetDisplayNameProcessPatch` | same family, but post-processes `ProcessFor` result | exact names, mixed modifier + JP base, bracketed states, liquid phrases | `GetDisplayNameRouteTranslator.TranslatePreservingColors(...)` in `GetDisplayNameProcessPatch` | downstream owner routes such as inventory, XDidY, AbilityBar target name fallback | `src/Patches/GetDisplayNameProcessPatch.cs`, `QudJP.Tests/L2/GetDisplayNameProcessPatchTests.cs`, runtime probes at `Player.log:509,520,567-572,601,618` | **Strongly supported** |
| Display-name consumers | `InventoryLocalizationPatch`, `XDidYTranslationPatch`, `DeathWrapperFamilyTranslator`, `AbilityBarAfterRenderTranslationPatch` target-name fallback | already-composed display-name fragments that may carry color or state suffixes | route-specific handoff to `GetDisplayNameRouteTranslator` | route-specific UI/message sinks | `ColorRouteCatalogTests.cs` inventory plus source callsites in appendix | **Source-traceable** |
| Zone display names | `ZoneManager.GetZoneDisplayName(...)` builds article/base/context/stratum text | plain strings with commas/strata text, later direct-marked by QudJP | `ZoneDisplayNameTranslationPatch.Postfix` -> `MessageFrameTranslator.MarkDirectTranslation(...)` | sink-side UITextSkin/message routes strip marker, do not retranslate | `src/Patches/ZoneDisplayNameTranslationPatch.cs`, `QudJP.Tests/L2/ZoneDisplayNameTranslationPatchTests.cs`, `/Users/sankenbisha/Dev/coq-decompiled/XRL.World/ZoneManager.cs:2254-2380`, runtime probes at `Player.log:264,476,594` | **Strongly supported** |
| Popup producer text: block/show/yes-no/ask-string/ask-number/show-space | callers into `XRL.UI.Popup.Show*` pass popup text; popup code wraps/render-preps later | plain English text, color-wrapped text, dynamic placeholders | producer route patches call `PopupTranslationPatch.TranslatePopupTextForProducerRoute(...)` before popup rendering | `PopupMessage.ShowPopup(...)` or popup text builder | `src/Patches/PopupShowTranslationPatch.cs`, `src/Patches/PopupAskStringTranslationPatch.cs`, `src/Patches/PopupAskNumberTranslationPatch.cs`, `src/Patches/PopupShowSpaceTranslationPatch.cs`, `src/Patches/PopupTranslationPatch.cs`, `QudJP.Tests/L2/PopupRouteHandoffTranslationTests.cs`, runtime probes at `Player.log:377-383,660` | **Strongly supported** |
| Popup message body/title/context/buttons/items | `Qud.UI.PopupMessage.ShowPopup(...)` receives already-translated payloads; game wraps body/title with markup | popup body text, title/context title, button/item texts like `{{W|[Esc]}} {{y|Cancel}}` | `PopupMessageTranslationPatch` rewrites args/items pre-show | `PopupMessage.Message.SetText(...)`, `Title.SetText(...)`, `contextText.SetText(...)` | `src/Patches/PopupMessageTranslationPatch.cs`, `/Users/sankenbisha/Dev/coq-decompiled/XRL.UI/Popup.cs:751-789`, `Qud.UI.PopupMessage` trace from librarian, `QudJP.Tests/L2/PopupRouteHandoffTranslationTests.cs`, runtime probes at `Player.log:382-383,525,576` | **Strongly supported** |
| Popup menu options (plain options/buttons) | `Popup.ShowConversation` / `PickOption` populate `QudMenuItem.text`; `QudMenuBottomContext` passes items to `SelectableTextMenuItem` | option labels and button labels, often later wrapped as `{{W|...}}` or `{{c|...}}` by selection state | producer-side translation in `PopupPickOptionTranslationPatch` and `QudMenuBottomContextTranslationPatch` | `SelectableTextMenuItem.SelectChanged(...)` -> `UITextSkin.SetText(...)` | `src/Patches/PopupPickOptionTranslationPatch.cs`, `src/Patches/QudMenuBottomContextTranslationPatch.cs`, `/Users/sankenbisha/Dev/coq-decompiled/XRL.UI/Popup.cs:1564-1705`, `/Users/sankenbisha/Dev/coq-decompiled/Qud.UI/QudMenuBottomContext.cs:22-42`, `/Users/sankenbisha/Dev/coq-decompiled/Qud.UI/SelectableTextMenuItem.cs:42-64`, `QudJP.Tests/L2/PopupRouteHandoffTranslationTests.cs` | **Source-traceable** |
| Popup menu options with embedded `{{hotkey|...}}` markup | game-produced option labels with inline hotkey placeholders | strings like `attac{{hotkey|k}}`, `{{hotkey|l}}ook`, `sho{{hotkey|w}} effects` | same producer patches as above | same popup/menu sink chain | runtime probes at `Player.log:709-712`; no matching `{{hotkey|...}}` L2 case was found in `PopupPickOptionTranslationPatchTests.cs` or `PopupRouteHandoffTranslationTests.cs` | **Runtime-required** |
| AbilityBar target text | `Qud.UI.AbilityBar.AfterRender(...)` builds `{{C|<color=#3e83a5>TARGET:</color> ...}}` using `currentTarget.DisplayName` | mixed legacy markup + literal TMP `<color>` + display-name payload | `AbilityBarAfterRenderTranslationPatch.TryTranslateTargetText(...)` | `TargetText.SetText(...)` (`UITextSkin`) | `src/Patches/AbilityBarAfterRenderTranslationPatch.cs`, `QudJP.Tests/L2/AbilityBarAfterRenderTranslationPatchTests.cs`, `/Users/sankenbisha/Dev/coq-decompiled/Qud.UI/AbilityBar.cs:373-421`, runtime probes at `Player.log:295,634,802` | **Strongly supported** |
| AbilityBar target health | `AbilityBar.AfterRender(...)` uses `Strings.WoundLevel(...)` plus feeling/difficulty fragments | compare-status text like `Fine, Hostile, Average` | `AbilityBarAfterRenderTranslationPatch.TryTranslateTargetHealthText(...)` | `TargetHealthText.SetText(...)` (`UITextSkin`) | same files as above; runtime probes at `Player.log:635,786` | **Strongly supported** |
| AbilityBar active effects (plain / simple) | `AbilityBar.AfterRender(...)` or `InternalUpdateActiveEffects(...)` builds `"{{Y|<color=#508d75>ACTIVE EFFECTS:</color>}} " + Markup.Wrap(effect)` and separators | comma-delimited effect descriptions, often legacy-wrapped | `AbilityBarAfterRenderTranslationPatch.TryTranslateEffectText(...)` | `EffectText.SetText(...)` (`UITextSkin`) | `src/Patches/AbilityBarAfterRenderTranslationPatch.cs`, `QudJP.Tests/L2/AbilityBarAfterRenderTranslationPatchTests.cs`, `/Users/sankenbisha/Dev/coq-decompiled/Qud.UI/AbilityBar.cs:256-327` | **Source-traceable** |
| AbilityBar active effects (TMP-colored mixed route) | same route, but with nested TMP spans around header/effects | `<color=...>ACTIVE EFFECTS:</color>` plus per-effect TMP spans and translated separators | same patch as above | same sink as above | branch diff + `QudJP.Tests/L2/AbilityBarAfterRenderTranslationPatchTests.cs` TMP case, but runtime log still shows malformed output at `Player.log:512,518` | **Runtime-required** |
| AbilityBar ability command / cycle command | `AbilityBar.UpdateAbilitiesText()` populates `AbilityCommandText` and `CycleCommandText` | command labels and page text; may include colors/hotkeys | `AbilityBarUpdateAbilitiesTextPatch` | `UITextSkin` fields on AbilityBar | `src/Patches/AbilityBarUpdateAbilitiesTextPatch.cs`, `/Users/sankenbisha/Dev/coq-decompiled/Qud.UI/AbilityBar.cs` | **Source-traceable** |
| `UITextSkinTranslationPatch` direct sink | final sink for many UI texts | already-assembled sink text such as `navigate`, `select`, LVL/HP/status lines, popup bodies | strips direct-marker if present, logs unclaimed text, returns source unchanged | `UITextSkin.SetText(...)` / TMP render path | `src/Patches/UITextSkinTranslationPatch.cs`, `QudJP.Tests/L2/UITextSkinTranslationPatchTests.cs`, runtime `SinkObserve/v1` lines throughout `Player.log` | **Unsupported/stop point** for fallback translation; **supported** only as observation |
| Sink-prereq UI methods | `SinkPrereqUiMethodTranslationPatch` observes/update-only UI methods across scrollers, status bar, trade screen, etc. | final sink text fields such as descriptions, zone text, trader names | `SinkPrereqTextFieldTranslator` forwards to `UITextSkinTranslationPatch.TranslatePreservingColors(...)`, which remains observation-only for sink contexts | `UITextSkin` fields | `src/Patches/SinkPrereqUiMethodTranslationPatch.cs`, `src/Patches/SinkPrereqTextFieldTranslator.cs`, runtime `SinkObserve/v1` entries like `Player.log:163,265,293` | **Unsupported/stop point** for ownership; **source-traceable** as observation |
| Direct TMP route outside shared sink path | game code writes literal TMP markup directly to `selectedAbilityText.text` | `<color=#...>` + `currentlySelectedAbility.DisplayName` | none in QudJP's shared `UITextSkin`/RTF path | `TextMeshProUGUI` direct assignment | `/Users/sankenbisha/Dev/coq-decompiled/GameManager.cs:326`, `/Users/sankenbisha/Dev/coq-decompiled/GameManager.cs:1947-1984` | **Unsupported/stop point** unless separately patched |

## Guarantee matrix

| Category | Included routes | What is actually guaranteed |
|---|---|---|
| **Strongly supported by source + tests** | helper strip/restore contract; `GetDisplayNamePatch`; `GetDisplayNameProcessPatch`; `ZoneDisplayNameTranslationPatch`; popup producer text and popup-message handoff; AbilityBar target text/target health | producer-owned translation preserves supported token forms and hands already-translated text downstream without relying on `UITextSkin` fallback |
| **Source-traceable but not fully guaranteed** | display-name consumer callsites from the route catalog; popup menu visual wrapper chain; plain AbilityBar active-effects route; AbilityBar ability command/cycle command; many generic `ColorAwareTranslationComposer` callsites in appendix | ownership and helper use are clear, but this audit did not find enough route-specific proof to claim end-to-end correctness |
| **Runtime-required for confidence** | AbilityBar TMP-colored active effects; popup options with inline `{{hotkey|...}}`; any mixed TMP/legacy route where the game adds extra wrapping later | current source/tests are not enough to overrule the need for fresh runtime evidence |
| **Unsupported / guarantee stops here** | sink-side `UITextSkin` fallback translation; sink-prereq observation routes; direct TMP assignments outside the shared pipeline; modern UI preservation of `^` background colors | repository intentionally does not own these routes, or the game-side conversion path discards the formatting |

## Highest-risk weak routes

1. **AbilityBar active effects with TMP-colored spans**
   - Evidence: branch diff and new L2 case exist, but fresh runtime log still shows malformed output at `Player.log:512,518`
   - Why weak: mixed legacy `{{...}}` and TMP `<color>` spans, plus per-effect segmentation and translated separators

2. **Popup pick-option text with embedded `{{hotkey|...}}` markup**
   - Evidence: runtime probes at `Player.log:709-712`
   - Why weak: no matching `{{hotkey|...}}` case was found in the audited L2 popup tests

3. **`UITextSkin` sink routes**
   - Evidence: repeated `SinkObserve/v1` entries for `navigate`, `select`, LVL/HP/status lines, popup bodies, prereq text (`Player.log:203-246`, `266-281`, `384`, `417`, `443-461`)
   - Why weak: code and tests prove these are observation-only, not fallback translation routes

4. **Any route that depends on `^` background colors surviving UI conversion**
   - Evidence: `Sidebar.FormatToRTF(...)` consumes `^x` markers without producing TMP output (`/Users/sankenbisha/Dev/coq-decompiled/XRL.UI/Sidebar.cs:691-701`)
   - Why weak: this is a game-side conversion loss, not a QudJP helper bug

5. **Direct TMP assignments outside the shared `UITextSkin`/RTF path**
   - Evidence: `GameManager.selectedAbilityText.text = ...` writes TMP `<color>` directly (`/Users/sankenbisha/Dev/coq-decompiled/GameManager.cs:1947-1984`)
   - Why weak: QudJP's shared ownership model does not reach this path

## Where support stops today

The repository currently supports **producer-owned translation routes that preserve color tokens before the text enters the renderer**. That support stops at four places:

1. **Sink ownership stops at `UITextSkin`**
   - `UITextSkinTranslationPatch` observes and strips direct-translation markers, but it does not perform general fallback translation for sink text.

2. **Render correctness stops at the game's own conversion path**
   - once text reaches `ToRTFCached()` / `RTF.FormatToRTF()` / `Sidebar.FormatToRTF()`, behavior is game-owned

3. **Background colors stop at `Sidebar.FormatToRTF()`**
   - `^` tokens are consumed and dropped in the modern UI conversion path

4. **Mixed TMP/legacy and direct-TMP routes still require runtime evidence**
   - tests can prove helper logic and route ownership, but they cannot alone prove the final on-screen behavior of mixed TMP/legacy producer strings

## Recommendations for next tests or fixes

No implementation was performed here. The next highest-value follow-ups are:

1. **Rebuild/sync the current branch and re-observe AbilityBar active effects**
   - confirm whether the current AbilityBar diff clears the malformed runtime shapes logged at `Player.log:512,518`

2. **Add L2 popup-option cases with embedded `{{hotkey|...}}` markup**
   - the runtime probes at `Player.log:709-712` are too important to leave unmodeled

3. **Add route-specific tests for `AbilityBarUpdateAbilitiesTextPatch`**
   - this route is cataloged and source-traceable, but it is weaker than the audited `AfterRender` fields

4. **Document the `^` background-color stop point explicitly**
   - the loss happens in game code, so future work should treat it as a renderer-boundary limitation, not a helper bug

## Appendix A — color-sensitive callsite inventory from `ColorRouteCatalogTests`

This appendix inventories every callsite currently enumerated by `QudJP.Tests/L1/ColorRouteCatalogTests.cs`. Presence here means the route is part of the repository's color-sensitive surface area; it does **not** by itself prove behavior.

### `ColorAwareTranslationComposer.TranslatePreservingColors(...)`

- `Mods/QudJP/Assemblies/src/Patches/AbilityBarUpdateAbilitiesTextPatch.cs`
- `Mods/QudJP/Assemblies/src/Patches/ActiveEffectTextTranslator.cs` (2)
- `Mods/QudJP/Assemblies/src/Patches/CharGenLocalizationPatch.cs`
- `Mods/QudJP/Assemblies/src/Patches/CharGenProducerTranslationHelpers.cs`
- `Mods/QudJP/Assemblies/src/Patches/CharacterEffectLineTranslationPatch.cs`
- `Mods/QudJP/Assemblies/src/Patches/CharacterStatusScreenBindingPatch.cs`
- `Mods/QudJP/Assemblies/src/Patches/CharacterStatusScreenMutationDetailsPatch.cs` (2)
- `Mods/QudJP/Assemblies/src/Patches/ConversationDisplayTextPatch.cs`
- `Mods/QudJP/Assemblies/src/Patches/CyberneticsTerminalTextTranslator.cs`
- `Mods/QudJP/Assemblies/src/Patches/DescriptionTextTranslator.cs`
- `Mods/QudJP/Assemblies/src/Patches/EquipmentLineTranslationPatch.cs`
- `Mods/QudJP/Assemblies/src/Patches/HelpRowTranslationPatch.cs`
- `Mods/QudJP/Assemblies/src/Patches/HighScoresScreenTranslationPatch.cs`
- `Mods/QudJP/Assemblies/src/Patches/HistoricStringExpanderPatch.cs`
- `Mods/QudJP/Assemblies/src/Patches/InventoryAndEquipmentStatusScreenTranslationPatch.cs`
- `Mods/QudJP/Assemblies/src/Patches/InventoryLineTranslationPatch.cs`
- `Mods/QudJP/Assemblies/src/Patches/JournalTextTranslator.cs`
- `Mods/QudJP/Assemblies/src/Patches/KeybindRowTranslationPatch.cs`
- `Mods/QudJP/Assemblies/src/Patches/KeybindsScreenTranslationPatch.cs`
- `Mods/QudJP/Assemblies/src/Patches/LoadingStatusTranslationPatch.cs`
- `Mods/QudJP/Assemblies/src/Patches/MainMenuLocalizationPatch.cs`
- `Mods/QudJP/Assemblies/src/Patches/MainMenuRowTranslationPatch.cs`
- `Mods/QudJP/Assemblies/src/Patches/PlayerStatusBarProducerTranslationHelpers.cs`
- `Mods/QudJP/Assemblies/src/Patches/MessageLogProducerTranslationHelpers.cs` (5)
- `Mods/QudJP/Assemblies/src/Patches/OptionsLocalizationPatch.cs`
- `Mods/QudJP/Assemblies/src/Patches/PickGameObjectScreenTranslationPatch.cs`
- `Mods/QudJP/Assemblies/src/Patches/SkillsAndPowersLineTranslationPatch.cs`
- `Mods/QudJP/Assemblies/src/Patches/SkillsAndPowersStatusScreenTranslationPatch.cs` (3)
- `Mods/QudJP/Assemblies/src/Patches/DeathReasonTranslationPatch.cs`
- `Mods/QudJP/Assemblies/src/Patches/UiBindingTranslationHelpers.cs`
- `Mods/QudJP/Assemblies/src/Patches/WorldCreationProgressTranslationPatch.cs`
- `Mods/QudJP/Assemblies/src/Patches/WorldGenerationScreenTranslationPatch.cs`

### `GetDisplayNameRouteTranslator.TranslatePreservingColors(...)`

- `Mods/QudJP/Assemblies/src/Patches/DeathWrapperFamilyTranslator.cs`
- `Mods/QudJP/Assemblies/src/Patches/GetDisplayNamePatch.cs`
- `Mods/QudJP/Assemblies/src/Patches/GetDisplayNameProcessPatch.cs`
- `Mods/QudJP/Assemblies/src/Patches/AbilityBarAfterRenderTranslationPatch.cs`
- `Mods/QudJP/Assemblies/src/Patches/InventoryLocalizationPatch.cs`
- `Mods/QudJP/Assemblies/src/Patches/XDidYTranslationPatch.cs`

### `PopupTranslationPatch` producer-route helpers

- `Mods/QudJP/Assemblies/src/Patches/PopupAskNumberTranslationPatch.cs` — `TranslatePopupTextForProducerRoute`
- `Mods/QudJP/Assemblies/src/Patches/PopupAskStringTranslationPatch.cs` — `TranslatePopupTextForProducerRoute`
- `Mods/QudJP/Assemblies/src/Patches/PopupMessageTranslationPatch.cs` — `TranslatePopupTextForProducerRoute`
- `Mods/QudJP/Assemblies/src/Patches/PopupPickOptionTranslationPatch.cs` — `TranslatePopupTextForProducerRoute` (2)
- `Mods/QudJP/Assemblies/src/Patches/PopupPickOptionTranslationPatch.cs` — `TranslatePopupMenuItemTextForProducerRoute`
- `Mods/QudJP/Assemblies/src/Patches/QudMenuBottomContextTranslationPatch.cs` — `TranslatePopupMenuItemTextForProducerRoute`
- `Mods/QudJP/Assemblies/src/Patches/TradeUiPopupTranslationPatch.cs` — `TranslatePopupTextForProducerRoute`
- `Mods/QudJP/Assemblies/src/Patches/PopupShowTranslationPatch.cs` — `TranslatePopupTextForProducerRoute`
- `Mods/QudJP/Assemblies/src/Patches/PopupShowSpaceTranslationPatch.cs` — `TranslatePopupTextForProducerRoute` (2)

### `UITextSkinTranslationPatch.TranslatePreservingColors(...)`

- `Mods/QudJP/Assemblies/src/Patches/SinkPrereqTextFieldTranslator.cs`

### Long-description / message-pattern / journal-pattern helpers

- `Mods/QudJP/Assemblies/src/Patches/DescriptionLongDescriptionPatch.cs` — `DescriptionTextTranslator.TranslateLongDescription`
- `Mods/QudJP/Assemblies/src/Patches/LookTooltipContentPatch.cs` — `DescriptionTextTranslator.TranslateLongDescription`
- `Mods/QudJP/Assemblies/src/Patches/DescriptionTextTranslator.cs` — `MessagePatternTranslator.Translate`
- `Mods/QudJP/Assemblies/src/Patches/MessageLogProducerTranslationHelpers.cs` — `MessagePatternTranslator.Translate` (4)
- `Mods/QudJP/Assemblies/src/Patches/PopupTranslationPatch.cs` — `MessagePatternTranslator.Translate`
- `Mods/QudJP/Assemblies/src/Patches/TradeUiPopupTranslationPatch.cs` — `MessagePatternTranslator.Translate`
- `Mods/QudJP/Assemblies/src/Patches/JournalTextTranslator.cs` — `JournalPatternTranslator.Translate` (2)

## Validation performed

- `dotnet test Mods/QudJP/Assemblies/QudJP.Tests/QudJP.Tests.csproj --filter "FullyQualifiedName~QudJP.Tests.L1.ColorRouteCatalogTests|FullyQualifiedName~QudJP.Tests.L1.ColorRestoreOwnershipTests|FullyQualifiedName~QudJP.Tests.L1.ColorCodePreserverTests|FullyQualifiedName~QudJP.Tests.L1.ColorMarkupTokenizerTests|FullyQualifiedName~QudJP.Tests.L2.AbilityBarAfterRenderTranslationPatchTests|FullyQualifiedName~QudJP.Tests.L2.UITextSkinTranslationPatchTests|FullyQualifiedName~QudJP.Tests.L2.PopupRouteHandoffTranslationTests|FullyQualifiedName~QudJP.Tests.L2.GetDisplayNameProcessPatchTests" --nologo`
