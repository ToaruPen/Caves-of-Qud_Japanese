# 2026-04-11 fixed-leaf / owner-side triage snapshot

## Scope

This snapshot records one scanner run on the `task/fixed-leaf-owner-triage` worktree created from `main` at `80b1162` (`Formalize proven fixed-leaf workflow (#353)`).

Command used:

```bash
python3 scripts/scan_text_producers.py \
  --source-root "/Users/sankenbisha/dev/coq-decompiled_stable" \
  --cache-dir .scanner-cache \
  --output docs/candidate-inventory.json \
  --phase all \
  --validate-fixed-leaf
```

## Scanner output summary

- Phase 1a: 5367 unique files, 3778 sink hits, 874 override hits
- Phase 1b: 4634 total sites
- Phase 1d: 4634 total sites, 2356 `translated`
- Fixed-leaf validation: **77 issues across 856 candidates**

Current `docs/candidate-inventory.json` totals:

- `translated`: 2356
- `unresolved`: 1199
- `needs_review`: 476
- `needs_patch`: 338
- `needs_translation`: 27
- `excluded`: 238

Type totals:

- `Leaf`: 856
- `Unresolved`: 2446
- `MessageFrame`: 339
- `VerbComposition`: 289
- `ProceduralText`: 242
- `Builder`: 164
- `NarrativeTemplate`: 130
- `Template`: 129
- `VariableTemplate`: 39

Ownership totals:

- `mid-pipeline-owned`: 856
- `producer-owned`: 1332
- `sink`: 2446

## Immediate fixed-leaf queue

The inventory currently exposes **27** non-translated `Leaf` sites with `destination_dictionary=global_flat`.

Observed shape:

- most keys are `""` or a single blank (`" "`)
- the remaining non-empty values are UI channel identifiers such as `BodyText` and `SelectedModLabel`
- none of the 27 are obviously ready-to-import player-visible text strings

Pending fixed-leaf sites:

| File | Line | Key | Pattern |
| --- | ---: | --- | --- |
| `Qud.UI/AchievementViewRow.cs` | 79 | `""` | `Date.SetText("")` |
| `Qud.UI/AchievementViewRow.cs` | 87 | `""` | `Date.SetText("")` |
| `Qud.UI/AchievementViewRow.cs` | 99 | `""` | `Date.SetText("")` |
| `Qud.UI/CharacterStatusScreen.cs` | 295 | `""` | `mutationsDetails.SetText("")` |
| `Qud.UI/CyberneticsTerminalRow.cs` | 65 | `""` | `description.SetText("")` |
| `Qud.UI/EquipmentLine.cs` | 377 | `""` | `hotkeyText.SetText("")` |
| `Qud.UI/EquipmentLine.cs` | 383 | `""` | `hotkeyText.SetText("")` |
| `Qud.UI/InventoryLine.cs` | 302 | `""` | `itemWeightText.SetText("")` |
| `Qud.UI/InventoryLine.cs` | 378 | `""` | `hotkeyText.SetText("")` |
| `Qud.UI/InventoryLine.cs` | 384 | `""` | `hotkeyText.SetText("")` |
| `Qud.UI/MissileWeaponAreaInfo.cs` | 67 | `""` | `text.SetText("")` |
| `Qud.UI/OptionsRow.cs` | 60 | `BodyText` | `TooltipTrigger.SetText("BodyText", RTF.FormatToRTF(data.HelpText))` |
| `Qud.UI/PopupMessage.cs` | 610 | `""` | `Title.SetText("")` |
| `Qud.UI/SkillsAndPowersStatusScreen.cs` | 154 | `""` | `requiredSkillsText.SetText("")` |
| `Qud.UI/TradeLine.cs` | 453 | `""` | `check.SetText("")` |
| `Qud.UI/TradeLine.cs` | 470 | `""` | `check.SetText("")` |
| `Qud.UI/WorldGenerationScreen.cs` | 223 | `" "` | `progressTexts[i].SetText(" ")` |
| `Qud.UI/WorldGenerationScreen.cs` | 229 | `""` | `attributionText.SetText("")` |
| `Qud.UI/WorldGenerationScreen.cs` | 230 | `""` | `quoteText.SetText("")` |
| `Qud.UI/WorldGenerationScreen.cs` | 245 | `""` | `quoteText.SetText("")` |
| `Qud.UI/WorldGenerationScreen.cs` | 246 | `""` | `attributionText.SetText("")` |
| `Qud.UI/WorldGenerationScreen.cs` | 252 | `""` | `attributionText.SetText("")` |
| `SteamScoresRow.cs` | 34 | `""` | `TextSkins[1].SetText("")` |
| `SteamWorkshopUploaderView.cs` | 121 | `SelectedModLabel` | `rootObject.transform.Find("SelectedModLabel").GetComponent<UITextSkin>().SetText("Managing - " + info.ID)` |
| `XRL.CharacterBuilds.Qud.UI/AttributeSelectionControl.cs` | 69 | `BodyText` | `tooltip.SetText("BodyText", Sidebar.FormatToRTF(data.BonusSource))` |
| `XRL.UI/Look.cs` | 293 | `BodyText` | `tooltip.SetText("BodyText", RTF.FormatToRTF(contents.ToString()))` |
| `XRL.UI/Look.cs` | 316 | `BodyText` | `trigger.SetText("BodyText", Sidebar.FormatToRTF(await GameManager.Instance.gameQueue.executeAsync(() => GenerateTooltipContent(item)))` |

### Reading of this queue

Under the `docs/RULES.md` proven fixed-leaf policy, this batch is currently **noise-heavy rather than import-ready**. It should be treated as a pruning/reclassification target before any dictionary registration work.

## Sink + owner-side mixed triage queue: highest-yield buckets

The non-fixed-leaf side of the inventory is much larger (**3778 sites**) and already groups naturally by route family. Note that this queue is **sink-observation + owner-side mixed**, not pure owner-side: the `Popup` and `AddPlayerMessage` rows shown below are `sink`-class sites that happen to be reached by existing route patches and marked `translated`, so they still need upstream owner proof before they can be promoted out of this queue. The 3778 count here equals the total non-fixed-leaf sites in the inventory (4634 total − 856 `Leaf` = 3778); it coincides with the Phase 1a sink-hit count on line 20 above, but it is computed as the post-Phase 1b residue, not as the raw Phase 1a sink hit tally.

Top buckets from the current run:

| Count | Source route | Type | Status | Notes |
| ---: | --- | --- | --- | --- |
| 761 | `Popup` | `Unresolved` | `translated` | sink-observed popup traffic; needs owner proof or upstream family mapping |
| 403 | `AddPlayerMessage` | `Unresolved` | `translated` | log/message queue traffic still classified at sink |
| 338 | `DidX` | `MessageFrame` | `needs_patch` | existing message-frame seam candidate |
| 283 | `Does` | `VerbComposition` | `needs_review` | owner-family review queue |
| 264 | `Parts.GetShortDescription` | `Unresolved` | `unresolved` | description-family upstream tracing |
| 238 | `HistoricStringExpander` | `ProceduralText` | `excluded` | explicitly procedural; not a dictionary target |
| 214 | `EmitMessage` | `Unresolved` | `unresolved` | producer-family split still needed |
| 180 | `Effects.GetDescription` | `Unresolved` | `unresolved` | effect description family |
| 171 | `Effects.GetDetails` | `Unresolved` | `unresolved` | effect details family |
| 143 | `GetDisplayName` | `Builder` | `translated` | builder/display-name route; not fixed-leaf |

Rejection-reason totals in the mixed triage queue (same sink+owner scope as above):

| Rejection reason | Count |
| --- | ---: |
| `unresolved` | 2446 |
| `message_frame` | 339 |
| `verb_composition` | 289 |
| `procedural` | 242 |
| `builder_display_name` | 164 |
| `narrative_template` | 130 |
| `template` | 129 |
| `variable_template` | 39 |

### Reading of this queue

For ongoing triage, the best return is to work route-family-first, not site-by-site:

1. `DidX` / message-frame (`needs_patch`)
2. `Does` / verb-composition (`needs_review`)
3. `EmitMessage` / `AddPlayerMessage` unresolved families
4. description families (`Parts.GetShortDescription`, `Effects.GetDescription`, `Effects.GetDetails`)

## Batch notes produced from this snapshot

The current route-family review notes created from this run are:

- `docs/reports/2026-04-11-didx-messageframe-batch-01.md`
- `docs/reports/2026-04-11-didx-prone-review.md`
- `docs/reports/2026-04-11-didx-firefighting-review.md`
- `docs/reports/2026-04-11-didx-holographicbleeding-review.md`
- `docs/reports/2026-04-11-didx-electricalgeneration-review.md`
- `docs/reports/2026-04-11-does-verbcomposition-batch-01.md`
- `docs/reports/2026-04-11-emit-addplayermessage-batch-01.md`
- `docs/reports/2026-04-11-description-families-batch-01.md`
- `docs/reports/2026-04-11-fixed-leaf-pruning-batch-01.md`

This means the snapshot is no longer only an inventory summary. It is now the umbrella entrypoint for the first concrete review batches across the highest-yield route-family triage queue (sink + owner-side mixed) and the fixed-leaf pruning queue.

## Validation blockers from this run

The fixed-leaf validator did not fail on subtle markup drift first; it failed on **duplicate exact keys**. Representative examples from stdout:

- `Your new pet is ready to love.`
- `That code is invalid.`
- `Choose a reward`
- `You cannot see your target.`
- `Nothing happens.`

This means the present work split should be:

1. prune obvious non-user-visible pseudo-leaf sites from the pending fixed queue
2. deduplicate repeated exact leaf candidates before any import attempt
3. spend review effort on owner-family buckets, not on the current 27 pseudo-leaf rows

## Worktree cleanup done alongside this run

Safely removed because each was clean, remote-gone, and patch-equivalent to `main`:

- `.worktrees/issue-345-ui-owner-routes` + local branch `issue-345-ui-owner-routes`
- `.worktrees/issue-346-relookup-routes` + local branch `issue-346-relookup-routes`

Intentionally kept because `git cherry main <branch>` still showed unique local commits:

- `issue-342-duplicate-dictionaries`
- `issue-343-status-description`
- `issue-344-cherubim-crash`
