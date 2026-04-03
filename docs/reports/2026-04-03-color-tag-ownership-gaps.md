# 2026-04-03 color-tag ownership gaps

## Scope

- Follow-up to `docs/reports/2026-04-03-color-tag-route-audit.md`
- Focus: color-sensitive routes that are **not yet explicitly owned** by QudJP as a producer or mid-pipeline route
- Evidence hierarchy: `Mods/QudJP/Assemblies/QudJP.Tests/` first, then repo source, then decompiled game source, then runtime notes from the prior audit

## Executive answer

The current tree has **fewer true ownership gaps than the prior route audit implied**. Most of the previously weak routes are now better described as **confidence gaps**: QudJP already owns them at a producer or mid-pipeline boundary, but some still need stronger route-specific proof or fresh runtime confirmation.

The **true non-owned surfaces** I could confirm are:

1. **Sink observation layers** that are intentionally non-owners and should stay that way: `UITextSkinTranslationPatch` and `SinkPrereqUiMethodTranslationPatch` (`Mods/QudJP/Assemblies/src/Patches/UITextSkinTranslationPatch.cs:65-119`, `Mods/QudJP/Assemblies/src/Patches/SinkPrereqUiMethodTranslationPatch.cs:18-30,128-148`).
2. **A direct-TMP producer outside the shared `UITextSkin` path**: `GameManager.UpdateSelectedAbility()` writes directly to `selectedAbilityText.text` (`/Users/sankenbisha/Dev/coq-decompiled/GameManager.cs:1941-1984`). This is the clearest route where QudJP ownership is still **missing but creatable** inside this repo.
3. **Renderer-side background-color loss**: `XRL.UI.Sidebar.FormatToRTF()` consumes `^x` background codes without emitting replacement output (`/Users/sankenbisha/Dev/coq-decompiled/XRL.UI/Sidebar.cs:691-701`). This is a **game-side boundary**, not a safe sink-side QudJP ownership target.

## Ownership gap vs. confidence gap

- **Ownership gap:** QudJP does not yet intercept the route at a safe producer or mid-pipeline point.
- **Confidence gap:** QudJP already intercepts the route at a safe point, but the route still lacks route-specific tests, runtime confirmation, or has a route-local implementation risk.

That distinction matters here because several routes from the prior audit are **already ownerized**:

- `AbilityBarUpdateAbilitiesTextPatch` owns `UpdateAbilitiesText()` and has route tests in `Mods/QudJP/Assemblies/QudJP.Tests/L2/SkillsAndAbilitiesOwnerPatchTests.cs:128-216`.
- `PopupPickOptionTranslationPatch` and `QudMenuBottomContextTranslationPatch` own plain popup/menu option flows in `Mods/QudJP/Assemblies/src/Patches/PopupPickOptionTranslationPatch.cs:81-186` and `Mods/QudJP/Assemblies/src/Patches/QudMenuBottomContextTranslationPatch.cs:33-103`, with L2 coverage in `PopupPickOptionTranslationPatchTests.cs`, `QudMenuBottomContextTranslationPatchTests.cs`, and `PopupRouteHandoffTranslationTests.cs`.
- `AbilityBarAfterRenderTranslationPatch` already owns active-effects/target text at `Mods/QudJP/Assemblies/src/Patches/AbilityBarAfterRenderTranslationPatch.cs:48-234`.
- Many appendix callsites are explicit row/binding owners because they set translated text through `OwnerTextSetter` before `UITextSkin` sees it (`Mods/QudJP/Assemblies/src/Patches/OwnerTextSetter.cs:7-23`), e.g. `InventoryLineTranslationPatch.cs:97-163`, `CharacterEffectLineTranslationPatch.cs:35-64`, `JournalLineTranslationPatch.cs:84-166`, `PickGameObjectLineTranslationPatch.cs:82-202`.

## Candidate universe reclassified

This table starts from the prior report's source-traceable, runtime-required, unsupported, and stop-point routes, then reclassifies them by **ownership**, not by confidence.

| Route family | Current best classification | Why | Key evidence |
|---|---|---|---|
| Display-name consumers (`InventoryLocalizationPatch`, `XDidYTranslationPatch`, `DeathWrapperFamilyTranslator`, AbilityBar target-name fallback) | **owner already exists** | The owning route is upstream `GetDisplayNamePatch` / `GetDisplayNameProcessPatch`, with consumer-side handoff where needed. | `GetDisplayNameRouteTranslator.cs:66-110`, `AbilityBarAfterRenderTranslationPatch.cs:184-203`, `SkillsAndAbilitiesOwnerPatchTests`/`XDidYTranslationPatchTests`/`UIExpansionPatchTests` noted during test mapping |
| Popup plain options/buttons | **owner already exists** | Producer-side ownership is explicit in `PopupPickOptionTranslationPatch` and bottom-context normalization. | `PopupPickOptionTranslationPatch.cs:81-186`, `QudMenuBottomContextTranslationPatch.cs:58-103`, `PopupPickOptionTranslationPatchTests.cs:51-213`, `QudMenuBottomContextTranslationPatchTests.cs:35-97`, `PopupRouteHandoffTranslationTests.cs:138-184` |
| Popup options with embedded `{{hotkey|...}}` | **owner already exists** (confidence gap) | The route already flows through popup producer helpers; the missing piece is route-specific popup proof for nested hotkey markup, not a missing owner boundary. | `PopupTranslationPatch.cs:325-383,1014-1063`, `PopupPickOptionTranslationPatch.cs:103-186`, prior runtime probes from `2026-04-03-color-tag-route-audit.md`; adjacent non-popup hotkey proof in `TinkeringTranslationPatchTests.cs:9-59` |
| AbilityBar active effects (plain) | **owner already exists** | `AbilityBarAfterRenderTranslationPatch` explicitly rewrites `effectText` after `AfterRender()`. | `AbilityBarAfterRenderTranslationPatch.cs:57-165`, `AbilityBarAfterRenderTranslationPatchTests.cs:43-72,102-135` |
| AbilityBar active effects (TMP-mixed) | **owner already exists** (confidence gap) | Same owner patch as plain effects; remaining weakness is mixed-TMP correctness, not missing ownership. | `AbilityBarAfterRenderTranslationPatch.cs:93-165`, `AbilityBarAfterRenderTranslationPatchTests.cs:138-158` |
| AbilityBar `UpdateAbilitiesText` | **owner already exists** | This route is already explicitly owned and route-tested in the current tree. | `AbilityBarUpdateAbilitiesTextPatch.cs:35-123`, `SkillsAndAbilitiesOwnerPatchTests.cs:128-216` |
| Generic appendix callsites that already set translated text before sink | **owner already exists** | These are explicit owner patches, usually row/binding patches that write through `OwnerTextSetter` or mark direct translation before sink. | `OwnerTextSetter.cs:7-23`; examples: `InventoryLineTranslationPatch.cs:97-163`, `CharacterEffectLineTranslationPatch.cs:35-64`, `JournalLineTranslationPatch.cs:84-166`, `PickGameObjectLineTranslationPatch.cs:82-202`, `WorldGenerationScreenTranslationPatch.cs:32-60`, `LoadingStatusTranslationPatch.cs:32-60` |
| `UITextSkin` direct sink fallback | **owner should not be created at sink** | This patch is explicitly observation-only and is not a safe mixed-route owner. | `UITextSkinTranslationPatch.cs:89-118`, `UITextSkinTranslationPatchTests.cs:39-79,190-254` |
| Sink-prereq UI methods | **owner should not be created at sink** | These methods intentionally forward to the observation-only sink path. | `SinkPrereqUiMethodTranslationPatch.cs:18-30,128-148`, `SinkPrereqTextFieldTranslator.cs:51-64`, `SinkPrereqTranslationPatchTests.cs:45-260` |
| Direct TMP assignment in `GameManager.UpdateSelectedAbility()` | **owner missing but creatable in producer** | This route bypasses `UITextSkin`, so a dedicated producer patch is the only safe owner inside QudJP. | `/Users/sankenbisha/Dev/coq-decompiled/GameManager.cs:1941-1984` |
| `^` background-color preservation in renderer conversion | **cannot be fully owned because the boundary is renderer-side** | The loss happens inside `Sidebar.FormatToRTF()`, after route ownership has ended. | `/Users/sankenbisha/Dev/coq-decompiled/XRL.UI/Sidebar.cs:650-717`, especially `691-701` |

## True ownership-gap catalog

These are the routes that are **not already owned** and therefore still need either a new explicit owner patch or a hard boundary statement.

| Route | Classification | Likely owner patch point or boundary | Required helper usage | Expected test shape | Main risks |
|---|---|---|---|---|---|
| `UITextSkin.SetText()` fallback sink | **owner should not be created at sink** | Do **not** ownerize `UITextSkinTranslationPatch`. If sink observation reveals an unowned route, patch the actual producer or binding method that feeds that sink text. | Use route-specific helpers upstream: `ColorAwareTranslationComposer`, `GetDisplayNameRouteTranslator`, `PopupTranslationPatch`, or `OwnerTextSetter` + `MessageFrameTranslator.MarkDirectTranslation`, depending on route shape. | L2 DummyTarget on the real producer/binding method; assert transformed text is already translated before sink and that `SinkObservation` hit count is zero for that route. | A sink-wide owner would see mixed-route traffic, double-translate already-owned text, and mis-handle dynamic or procedural text that only looks stable at the sink. |
| `SinkPrereqUiMethodTranslationPatch` target set (`CategoryMenusScroller.UpdateDescriptions`, `FrameworkScroller.BeforeShow`, `HorizontalScroller.BeforeShow`, `TitledIconButton.Update`, `CyberneticsTerminalRow.Update`, `AbilityManagerScreen.HandleHighlightLeft`, `TradeScreen.HandleHighlightObject`, `MapScrollerPinItem.SetData`, `PlayerStatusBar.Update`, `TradeScreen.UpdateTitleBars`) | **owner should not be created at sink** | Keep `SinkPrereqUiMethodTranslationPatch` as observation. For any concrete uncovered route, add a dedicated producer or binding patch on the specific row/screen method that populates the observed field, following existing owner-patch patterns such as `InventoryLineTranslationPatch`, `JournalLineTranslationPatch`, `PickGameObjectLineTranslationPatch`, `CharacterEffectLineTranslationPatch`. | Prefer `OwnerTextSetter` for UI field owners; use route-local translators plus `ColorAwareTranslationComposer` when the source mixes colors with visible text. | L2 per affected control/binding method, plus `SinkObservation` assertions proving the sink observer no longer sees untranslated source for that route. | Broad sink-prereq ownership would collapse many heterogeneous UI fields into one unsafe fallback path. |
| `GameManager.UpdateSelectedAbility()` direct TMP assignment to `selectedAbilityText.text` | **owner missing but creatable in producer** | Add a dedicated patch on `GameManager.UpdateSelectedAbility()` (`/Users/sankenbisha/Dev/coq-decompiled/GameManager.cs:1941-1984`) or an immediately adjacent helper it calls. This is the safest route-local owner because the text never reaches `UITextSkin`. | TMP-aware preservation using `ColorAwareTranslationComposer.Strip/Restore` or `TranslatePreservingColors` over the final built string; reuse route-local translation for command prompts and `GetDisplayNameRouteTranslator` if the ability display name needs display-name semantics. | L2G target-resolution test for `GameManager.UpdateSelectedAbility`, plus L2 DummyTarget that mimics the final TMP string and asserts direct `TextMeshProUGUI.text` ownership without using sink fallback. | Direct TMP nesting, cooldown suffix placement, control-input fragments, and no safety net from `UITextSkin` marker stripping. |
| `XRL.UI.Sidebar.FormatToRTF()` background-color loss | **cannot be fully owned because the boundary is renderer-side** | Treat as a renderer boundary. A route-local owner patch cannot preserve `^x` once the string reaches `Sidebar.FormatToRTF()`. Fixing it would require a global renderer/game patch, not a safe per-route QudJP owner. | Not applicable for route ownership. | If ever attempted, it would need renderer-focused L2G/L3 evidence, not ordinary route-owner tests. | Global behavior change across mixed UI routes, and the patch would no longer be a route-local ownership decision. |

## Already-owned but still weaker-confidence routes

These are important because they can still produce color-tag bugs, but those bugs are **not caused by missing ownership**.

| Route | Why it is not an ownership gap | What is still weak |
|---|---|---|
| Popup options with embedded `{{hotkey|...}}` | Producer ownership already exists in popup route helpers. `ColorAwareTranslationComposer.Strip()` removes the outer markup, and `PopupTranslationPatch` already recognizes localized hotkey-label shapes (`PopupTranslationPatch.cs:1014-1063`). | Popup-route-specific L2 proof is still missing for nested `{{hotkey|...}}` labels after game-added wrappers such as `SelectableTextMenuItem.SelectChanged()` (`/Users/sankenbisha/Dev/coq-decompiled/Qud.UI/SelectableTextMenuItem.cs:42-64`). |
| AbilityBar active effects with mixed TMP + legacy markup | `AbilityBarAfterRenderTranslationPatch` already owns `effectText`. | Mixed TMP/legacy composition can still break because the game builds colored strings in `AbilityBar.AfterRender()` / `InternalUpdateActiveEffects()` before final renderer conversion (`/Users/sankenbisha/Dev/coq-decompiled/Qud.UI/AbilityBar.cs:256-327`). |
| Appendix callsites with thinner route tests | Many are already explicit row/binding owners via `OwnerTextSetter` or direct producer rewrites. | Some helper-backed routes still only have indirect or adjacent proof; that is a coverage gap, not an ownership gap. |

## Would ownerizing every creatable route eliminate QudJP-side color-tag bugs?

**No.** It would eliminate the subset of bugs caused by **missing explicit ownership**, but it would not eliminate either renderer-side losses or route-local bugs inside existing owner patches.

### Bugs that ownerizing all creatable routes would address

- Missing ownership on direct producers that bypass the shared sink, such as `GameManager.UpdateSelectedAbility()`
- Future sink-observed routes that are later traced to a stable producer or binding method and patched there

### Residual bug classes that would still remain afterward

1. **Renderer/game-side losses**
   - `^` background colors dropped by `Sidebar.FormatToRTF()` (`/Users/sankenbisha/Dev/coq-decompiled/XRL.UI/Sidebar.cs:691-701`)
   - Any other behavior inside `UITextSkin.SetText() -> Apply() -> ToRTFCached() -> RTF.FormatToRTF() -> Sidebar.FormatToRTF()` (`/Users/sankenbisha/Dev/coq-decompiled/XRL.UI/UITextSkin.cs:116-176`, `/Users/sankenbisha/Dev/coq-decompiled/Extensions.cs:592-599`, `/Users/sankenbisha/Dev/coq-decompiled/Qud.UI/RTF.cs:9-23`)

2. **Route-local bugs inside already-owned patches**
   - Incorrect slice/restoration logic when separators or nested tags change shape after translation
   - Mixed TMP + legacy composition bugs such as the still-runtime-sensitive AbilityBar active-effects route

3. **Confidence gaps**
   - Routes that already have owners but still lack the exact L2/L3 proof needed to claim full confidence, especially popup `{{hotkey|...}}` composition

So the correct claim is:

> **Systematic ownerization can eliminate missing-owner bugs, but not all QudJP-side color-tag bugs.**

It does not fix renderer-side losses, and it does not automatically fix bugs inside owner patches that already exist.

## Validation performed

- `dotnet build Mods/QudJP/Assemblies/QudJP.csproj --nologo`
- `dotnet test Mods/QudJP/Assemblies/QudJP.Tests/QudJP.Tests.csproj --filter "FullyQualifiedName~QudJP.Tests.L1.ColorRouteCatalogTests|FullyQualifiedName~QudJP.Tests.L2.PopupPickOptionTranslationPatchTests|FullyQualifiedName~QudJP.Tests.L2.QudMenuBottomContextTranslationPatchTests|FullyQualifiedName~QudJP.Tests.L2.PopupRouteHandoffTranslationTests|FullyQualifiedName~QudJP.Tests.L2.AbilityBarAfterRenderTranslationPatchTests|FullyQualifiedName~QudJP.Tests.L2.SkillsAndAbilitiesOwnerPatchTests|FullyQualifiedName~QudJP.Tests.L2.UITextSkinTranslationPatchTests|FullyQualifiedName~QudJP.Tests.L2.SinkPrereqTranslationPatchTests" --nologo`

## Bottom line

The ownership-gap audit narrows to a small set:

- **Do not ownerize sinks** (`UITextSkin`, sink-prereq observers)
- **Do ownerize direct TMP producers route-by-route if needed** (`GameManager.UpdateSelectedAbility()` is the clearest current example)
- **Treat renderer conversion loss as a game boundary**, not a missing QudJP owner
- **Keep separate the already-owned routes that still need confidence work** (popup hotkey composition, mixed TMP AbilityBar effects)
