# Issue 528 Visible Fallback Inventory

## Scope

Issue #528 audits runtime localization fallbacks that can reach player-visible
text and make an owner/source-route gap look translated. The static pass used:

- `rg` over `Mods/QudJP/Assemblies/src/Patches` and `src/Translation` for
  fallback vocabulary, `DisplayName` reflection, id/blueprint labels,
  `TryGetTranslationExactOrLowerAscii`, and `TranslateExactOrLowerAsciiFallback`.
- `just sg-cs '$VALUE.ToString()' Mods/QudJP/Assemblies/src/Patches` for
  structural `ToString()` call candidates.
- Decompiled route checks under `~/dev/coq-decompiled_stable` for
  `XRL.World.Capabilities.Messaging.XDidY*` and `GameObject.one/One`.

## Fixed In This Batch

- `XDidYTranslationPatch.GetEntityDisplayName`
  - Classification: masking bug.
  - Previous behavior: when `one/One` could not be invoked, QudJP used
    `ToString()` as a player-visible display name and dispatched a translated
    message.
  - New behavior: QudJP logs a route-specific warning and returns to the
    original game message route instead of treating fallback text as coverage.
  - Tests: subject, single-object, double-object, and missing typed route cases.

- `FactionsLineDataTranslationPatch`
  - Classification: masking bug.
  - Previous behavior: if the generated label could not be translated, the
    line-data patch called
    `FactionsStatusScreenTranslationPatch.TryTranslateFactionLabelFromId`,
    translated the faction id, and used that id translation as the visible
    label.
  - New behavior: generated labels remain unchanged unless the label/source
    text itself is translated. The id translation is no longer a visible-label
    fallback.
  - Tests: generated labels do not use faction id fallback or record a
    `FactionLabelFallback` transform.

- `InventoryLineTranslationPatch`
  - Classification: route update.
  - Previous behavior: item names used a broad exact/lower lookup path.
  - New behavior: item names use `GetDisplayNameRouteTranslator`, so generated
    display-name handling and scoped display-name dictionaries own the route.
  - Tests: item names prefer display-name scoped atomic entries over global
    exact entries.

- `GetDisplayNameProcessPatchTests`
  - Classification: test terminology update.
  - The `CanvasWall` test now describes an observed atomic display name instead
    of blessing it as a blueprint fallback.

## Retained Or Deferred Candidates

- `PopupTranslationPatch` unclaimed sink behavior
  - Classification: intentional observability.
  - It logs unclaimed sink text and returns the source. It does not claim
    localization coverage.

- `ActiveEffectTextTranslator` generated capture fallback
  - Classification: safety fallback with warning.
  - `TranslateExactOrLowerAsciiFallback` keeps the source value visible and logs
    a warning. A later owner pass can decide whether missing captures should
    fail the whole generated template instead of partially translating.

- `CharacterEffectLineTranslationPatch`
  - Classification: deferred fixed-leaf/owner review.
  - Effect display names are currently translated through exact/lower lookup.
    This may be correct for fixed effect leaves, but generated effect labels
    need a separate owner-route proof before changing behavior.

- `GetDisplayNameProcessPatch` atomic exact lookups
  - Classification: display-name owner route.
  - The display-name process patch is already an owner route for object display
    names. Exact atomic entries are retained, but tests should avoid calling
    them blueprint fallback coverage.

## Remaining Static Candidates

The broad exact/lower helper appears in many UI owner patches. These are not
automatically bugs. Future cleanup should only change a route when static route
evidence shows that the helper is masking generated/player-visible text rather
than translating a fixed UI label or a tested producer-owned family.
