# CJK Font Injection — TMP Global Fallback Registration

## TL;DR

> **Quick Summary**: Inject a subset Noto Sans CJK JP font into TextMeshPro's global fallback at mod startup so Japanese text renders visibly. Rewrite the 19-line no-op FontManager.cs into a ~60-line implementation that loads an OTF, creates a dynamic TMP_FontAsset, and registers it as a single global fallback entry.
>
> **Deliverables**:
> - `Mods/QudJP/Fonts/NotoSansCJKjp-Regular-Subset.otf` — Subset CJK font (~3-5MB)
> - `Mods/QudJP/Assemblies/src/FontManager.cs` — Rewritten with TMP font injection
> - Updated `QudJP.csproj` with conditional TMP DLL references
> - Updated `sync_mod.py` and `build_release.py` for Fonts/ deployment
> - Player.log evidence of successful font loading
> - Japanese text visually confirmed in game UI
>
> **Estimated Effort**: Short (2-3 hours implementation + manual game verification)
> **Parallel Execution**: YES — 3 waves + verification
> **Critical Path**: Task 1 (font prep) → Task 4 (FontManager rewrite) → Task 5 (build+deploy) → Task 6 (L3 verify)

---

## Context

### Problem Statement

Bootstrap.cs works correctly (Player.log confirms 3/3 messages), 11 Harmony patches are applied, and XML translations are merged — but Japanese text is **invisible**. The game's default font (LiberationSans SDF) contains no CJK glyphs. TextMeshPro renders missing glyphs as invisible/tofu characters.

### Root Cause (Verified)

CoQ uses TextMeshPro for all modern UI. Developer AlphaBeard confirmed (2024-12-28): "The character set is no longer a bitmap font, the modern UI is fully translatable technically." The game CAN render CJK — it just needs a CJK font asset registered in TMP's fallback chain.

### Oracle Critical Analysis of Legacy FontManager

The legacy project's 505-line FontManager was analyzed and classified:

**ESSENTIAL (keep)**:
- 1 font file (Regular only — TMP handles fake bold via SDF scaling)
- `TMP_FontAsset.CreateFontAsset(path, 0, 96, 6, GlyphRenderMode.SDFAA, 4096, 4096)`
- `AtlasPopulationMode.Dynamic` + `isMultiAtlasTexturesEnabled = true`
- `TMP_Settings.fallbackFontAssets` — ONE entry, global registration
- Init-time probe validation with `TryAddCharacters`, throw on failure

**DROP (over-engineered)**:
- Bold font (2nd file), VanillaFontHints detection, per-asset fallback chain injection
- Font weight table configuration, Symbol/sprite fallback capture
- ApplyToText() cosmetic adjustments, Legacy UI.Text handling, IDisposable cleanup
- 11+ font guard patches (OnEnable hooks)

**User's Directive on Fallbacks**:
> "フォールバックは設ける必要があるのでしょうか？トレースするのが困難になりません？"
→ Global TMP fallback is necessary (it's how TMP discovers CJK glyphs), but fallback CHAINS are not. A single entry in global fallback is sufficient. Deep chains make debugging harder.

### Technical Approach: `#if HAS_TMP` Conditional Compilation

The mod DLL (net48) builds locally WITH game DLLs and on CI WITHOUT game DLLs. Since Unity.TextMeshPro has no NuGet package, we use conditional compilation:

- **Local build (HAS_TMP defined)**: Full TMP API access, direct font injection code
- **CI build (HAS_TMP not defined)**: FontManager is a safe no-op, all tests pass
- **Runtime**: Always uses local build, TMP is always available

This follows the existing pattern where 0Harmony has conditional/NuGet fallback in the csproj.

---

## Work Objectives

### Core Objective

Make Japanese text visible in Caves of Qud by injecting a CJK font into TMP's global fallback chain at mod startup.

### Concrete Deliverables

- Subset CJK font file committed to repo
- FontManager.cs rewritten from no-op to functional implementation
- Deployment tooling updated to include Fonts/
- Japanese text confirmed visible in game UI

### Definition of Done

- [ ] `Mods/QudJP/Fonts/NotoSansCJKjp-Regular-Subset.otf` exists and is < 6MB
- [ ] `dotnet build Mods/QudJP/Assemblies/QudJP.csproj` succeeds with 0 warnings (local + CI)
- [ ] `dotnet test Mods/QudJP/Assemblies/QudJP.Tests/QudJP.Tests.csproj` — all tests pass
- [ ] `pytest scripts/tests/` — all tests pass
- [ ] Player.log contains `[QudJP] FontManager:` success messages
- [ ] Japanese text visible in at least 3 UI elements (Options menu, Popup, Message Log)

### Must Have

- Single CJK font file (Regular weight only, Noto Sans CJK JP subset)
- `TMP_FontAsset.CreateFontAsset` with Dynamic atlas mode
- ONE entry in `TMP_Settings.fallbackFontAssets` (global fallback)
- Probe validation (`TryAddCharacters`) with fail-fast on failure
- `UnityEngine.Debug.Log` for Player.log visibility (not System.Diagnostics.Trace)
- SIL OFL license file included with font
- CI build compatibility (no-op path when game DLLs unavailable)
- Font path resolution reusing `LocalizationAssetResolver` pattern (Assembly.Location → parent → Fonts/)

### Must NOT Have (Guardrails)

- Bold font (TMP handles fake bold via SDF thickness)
- `TMP_Settings.defaultFontAsset` replacement (only add to fallback, don't replace default)
- Per-asset fallback chain injection (`Resources.FindObjectsOfTypeAll` + loop)
- Font guard Harmony patches (OnEnable hooks for TMP_Text, etc.)
- Console overlay (`ConsoleBridge` / `ConsoleBridgeView`)
- Deep fallback chains (user explicitly rejected — "トレースするのが困難")
- Font weight table manipulation
- Sprite asset registration
- Legacy `UnityEngine.UI.Text` font replacement
- Any code copied from legacy FontManager.cs (design reference only)

---

## Verification Strategy

> **ZERO HUMAN INTERVENTION for automated checks** — L3 game verification requires manual game launch.

### Test Decision

- **Infrastructure exists**: YES (NUnit + pytest)
- **Automated tests**: Tests-after for path resolution logic; FontManager TMP calls are L3 only
- **Framework**: NUnit (C#), pytest (Python)

### QA Policy

- L1: Path resolution logic, font file existence checks
- L3: Game launch → Player.log inspection → visual confirmation of Japanese text
- Evidence: `.sisyphus/evidence/font-*.txt` for automated, Player.log for L3

---

## Execution Strategy

### Parallel Execution Waves

```
Wave 1 (Start Immediately — foundation, 3 parallel):
├── Task 1: Font file preparation (glyphset + subset script + OTF) [quick]
├── Task 2: Add TMP/Unity DLL references to QudJP.csproj [quick]
└── Task 3: Update deployment tooling for Fonts/ [quick]

Wave 2 (After Wave 1 — core implementation):
└── Task 4: Rewrite FontManager.cs with CJK font injection [deep]

Wave 3 (After Wave 2 — integration):
└── Task 5: Build, test, deploy [quick]

Wave 4 (After Wave 3 — verification):
└── Task 6: L3 game verification [manual]

Wave 5 (CONDITIONAL — only if L3 reveals issues):
└── Task 7: Targeted screen patches [deep] (scope TBD by L3 results)

Critical Path: Task 1 → Task 4 → Task 5 → Task 6
Parallel Speedup: ~40% faster than sequential (Wave 1 parallelism)
```

### Dependency Matrix

| Task | Depends On | Blocks | Wave |
|------|-----------|--------|------|
| 1 | — | 4, 5 | 1 |
| 2 | — | 4 | 1 |
| 3 | — | 5 | 1 |
| 4 | 1, 2 | 5 | 2 |
| 5 | 3, 4 | 6 | 3 |
| 6 | 5 | 7 (conditional) | 4 |
| 7 | 6 (only if needed) | — | 5 |

### Agent Dispatch Summary

| Wave | Tasks | Categories |
|------|-------|-----------|
| 1 | 3 | T1 → quick, T2 → quick, T3 → quick |
| 2 | 1 | T4 → deep |
| 3 | 1 | T5 → quick |
| 4 | 1 | T6 → manual (user) |
| 5 | 0-1 | T7 → deep (conditional) |

---

## TODOs

### Wave 1: Foundation (3 parallel tasks)

- [x] 1. Font File Preparation — Glyphset, Subset Script, and OTF Generation

  **What to do**:
  - Copy `glyphset.txt` from legacy `Docs/glyphset.txt` to `docs/glyphset.txt` (28 lines, Unicode range definitions for Basic Latin, Hiragana, Katakana, CJK Unified Ideographs U+4E00-9FFF, Fullwidth forms, Misc symbols)
  - Create `scripts/subset_font.py`:
    - Downloads NotoSansCJKjp-Regular.otf from Google Fonts GitHub release (https://github.com/googlefonts/noto-cjk/releases) to a temp directory
    - Runs `fonttools.subset` with `--unicodes-file=docs/glyphset.txt --layout-features=* --output-file=Mods/QudJP/Fonts/NotoSansCJKjp-Regular-Subset.otf`
    - Validates output file exists and is < 6MB
    - CLI: `python scripts/subset_font.py` (idempotent, safe to rerun)
  - Add `fonttools` and `brotli` to `pyproject.toml` optional dependencies (`[project.optional-dependencies] fonts = ["fonttools", "brotli"]`)
  - Run the script to generate the subset font file
  - Create `Mods/QudJP/Fonts/OFL.txt` — copy of SIL Open Font License 1.1 (required for redistribution)
  - Verify subset file size is reasonable (expect 3-5MB)
  - Commit subset font and license to git (regular git, no LFS needed for <6MB)

  **Must NOT do**:
  - Commit the full source NotoSansCJKjp-Regular.otf to the repo (only the subset)
  - Include Bold font (Oracle explicitly dropped it)
  - Copy any code from legacy subset scripts

  **Recommended Agent Profile**:
  - **Category**: `quick`
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1 (with Tasks 2, 3)
  - **Blocks**: Tasks 4, 5
  - **Blocked By**: None

  **References**:

  **Pattern References**:
  - `/Users/sankenbisha/Dev/Caves-of-Qud_Japanese_legacy/Docs/glyphset.txt` — Unicode range definitions to copy (28 lines: Basic Latin, Latin-1, Hiragana, Katakana, CJK Unified Ideographs, Fullwidth, Misc)
  - `/Users/sankenbisha/Dev/Caves-of-Qud_Japanese_legacy/Docs/font_pipeline.md:18-36` — Subset command reference (design only; we use macOS `python3 -m fontTools.subset` not PowerShell `py`)

  **External References**:
  - Noto CJK releases: `https://github.com/googlefonts/noto-cjk/releases` — Download source OTF
  - SIL OFL 1.1 license text: `https://openfontlicense.org/open-font-license-official-text/`
  - fonttools subset docs: `https://fonttools.readthedocs.io/en/latest/subset/`

  **Acceptance Criteria**:
  - [ ] `docs/glyphset.txt` exists with Unicode ranges including U+4E00-9FFF (CJK Unified Ideographs)
  - [ ] `scripts/subset_font.py` exists and `ruff check scripts/subset_font.py` passes
  - [ ] `Mods/QudJP/Fonts/NotoSansCJKjp-Regular-Subset.otf` exists
  - [ ] Font file size < 6MB: `ls -lh Mods/QudJP/Fonts/NotoSansCJKjp-Regular-Subset.otf`
  - [ ] `Mods/QudJP/Fonts/OFL.txt` exists with SIL Open Font License text
  - [ ] `python3 -c "from fontTools.ttLib import TTFont; f=TTFont('Mods/QudJP/Fonts/NotoSansCJKjp-Regular-Subset.otf'); print(f'Glyphs: {len(f.getGlyphOrder())}')"` — prints glyph count

  **QA Scenarios**:
  ```
  Scenario: Subset font contains required Unicode ranges
    Tool: Bash
    Steps:
      1. python3 -c "from fontTools.ttLib import TTFont; f=TTFont('Mods/QudJP/Fonts/NotoSansCJKjp-Regular-Subset.otf'); cmap=f.getBestCmap(); print('Hiragana:', 0x3042 in cmap); print('Katakana:', 0x30A2 in cmap); print('Kanji:', 0x65E5 in cmap)"
         → expect Hiragana: True, Katakana: True, Kanji: True (日=U+65E5)
      2. ls -lh Mods/QudJP/Fonts/NotoSansCJKjp-Regular-Subset.otf → size < 6MB
      3. test -f Mods/QudJP/Fonts/OFL.txt && echo "License present"
    Expected Result: Font contains CJK glyphs, is under 6MB, license included
    Evidence: .sisyphus/evidence/font-subset-validation.txt

  Scenario: subset_font.py lint clean
    Tool: Bash
    Steps:
      1. ruff check scripts/subset_font.py → exit 0
      2. python3 scripts/subset_font.py --help → prints usage
    Expected Result: Lint clean, help output visible
    Evidence: .sisyphus/evidence/font-script-lint.txt
  ```

  **Commit**: YES
  - Message: `feat(fonts): add CJK font subset and generation script`
  - Files: `docs/glyphset.txt`, `scripts/subset_font.py`, `Mods/QudJP/Fonts/NotoSansCJKjp-Regular-Subset.otf`, `Mods/QudJP/Fonts/OFL.txt`, `pyproject.toml`

- [x] 2. Add TMP/Unity DLL References to QudJP.csproj

  **What to do**:
  - Add a conditional `<PropertyGroup>` that defines `HAS_TMP` when `Unity.TextMeshPro.dll` exists:
    ```xml
    <PropertyGroup Condition="Exists('$(ManagedDir)/Unity.TextMeshPro.dll')">
      <DefineConstants>$(DefineConstants);HAS_TMP</DefineConstants>
    </PropertyGroup>
    ```
  - Add a conditional `<ItemGroup>` with TMP and Unity DLL references (all `<Private>false</Private>`):
    - `Unity.TextMeshPro.dll` — TMP_FontAsset, TMP_Settings, AtlasPopulationMode
    - `UnityEngine.CoreModule.dll` — UnityEngine.Debug for Player.log visibility
    - `UnityEngine.TextCoreFontEngineModule.dll` — FontEngine, GlyphRenderMode
    - `UnityEngine.TextCoreTextEngineModule.dll` — TextCore types
  - Place these AFTER the existing `<!-- Game DLL references -->` comment at line 49
  - Verify `dotnet build` succeeds locally (with game DLLs)
  - Verify the existing CI would still succeed (HAS_TMP not defined when DLLs absent — no new compile errors in non-HAS_TMP code paths)

  **Must NOT do**:
  - Add NuGet packages for TMP (none exist)
  - Make TMP references unconditional (CI would break)
  - Reference Assembly-CSharp.dll (not needed for font injection)
  - Change existing 0Harmony conditional reference pattern

  **Recommended Agent Profile**:
  - **Category**: `quick`
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1 (with Tasks 1, 3)
  - **Blocks**: Task 4
  - **Blocked By**: None

  **References**:

  **Pattern References**:
  - `Mods/QudJP/Assemblies/QudJP.csproj:31-36` — Existing conditional 0Harmony reference pattern (follow same Condition + HintPath + Private=false pattern)
  - `Mods/QudJP/Assemblies/QudJP.csproj:13-15` — GameDir/ManagedDir properties (reuse `$(ManagedDir)` for HintPath)

  **API/Type References**:
  - Game DLL directory: `~/Library/Application Support/Steam/steamapps/common/Caves of Qud/CoQ.app/Contents/Resources/Data/Managed/`
  - Required DLLs: `Unity.TextMeshPro.dll`, `UnityEngine.CoreModule.dll`, `UnityEngine.TextCoreFontEngineModule.dll`, `UnityEngine.TextCoreTextEngineModule.dll`

  **Acceptance Criteria**:
  - [ ] `dotnet build Mods/QudJP/Assemblies/QudJP.csproj` succeeds with 0 warnings (local, game DLLs present)
  - [ ] `grep "HAS_TMP" Mods/QudJP/Assemblies/QudJP.csproj` → found in DefineConstants
  - [ ] `grep "Unity.TextMeshPro" Mods/QudJP/Assemblies/QudJP.csproj` → found in Reference
  - [ ] `grep "Private>false" Mods/QudJP/Assemblies/QudJP.csproj` → appears for each new reference

  **QA Scenarios**:
  ```
  Scenario: Local build succeeds with TMP references
    Tool: Bash
    Steps:
      1. dotnet build Mods/QudJP/Assemblies/QudJP.csproj -c Release → exit 0, 0 warnings
      2. dotnet build Mods/QudJP/Assemblies/QudJP.csproj -c Release 2>&1 | grep -c "error\|warning" → 0
    Expected Result: Clean build with TMP references available
    Evidence: .sisyphus/evidence/font-csproj-build.txt

  Scenario: csproj has conditional TMP references
    Tool: Bash (grep)
    Steps:
      1. grep -c "HAS_TMP" Mods/QudJP/Assemblies/QudJP.csproj → ≥1
      2. grep -c "Unity.TextMeshPro" Mods/QudJP/Assemblies/QudJP.csproj → ≥1
      3. grep -c "UnityEngine.CoreModule" Mods/QudJP/Assemblies/QudJP.csproj → ≥1
      4. grep -c "TextCoreFontEngineModule" Mods/QudJP/Assemblies/QudJP.csproj → ≥1
    Expected Result: All TMP references present in csproj
    Evidence: .sisyphus/evidence/font-csproj-refs.txt
  ```

  **Commit**: NO (groups with Task 4)

- [x] 3. Update Deployment Tooling for Fonts/ Directory

  **What to do**:

  **sync_mod.py** (`scripts/sync_mod.py:14-21`):
  - Add `"Fonts/"` and `"Fonts/**"` to `_RSYNC_INCLUDES` tuple, after `"Localization/**"`:
    ```python
    _RSYNC_INCLUDES: tuple[str, ...] = (
        "manifest.json",
        "Bootstrap.cs",
        "Assemblies/",
        "Assemblies/QudJP.dll",
        "Localization/",
        "Localization/**",
        "Fonts/",
        "Fonts/**",
    )
    ```

  **build_release.py** (find the ZIP creation function):
  - Add `Fonts/` directory to the files included in the release ZIP
  - Ensure font file and OFL.txt are both included

  **docs/deployment.md**:
  - Add `Fonts/` to the "Deployed Files" table
  - Add `Fonts/NotoSansCJKjp-Regular-Subset.otf` — CJK font for TMP rendering
  - Add `Fonts/OFL.txt` — SIL Open Font License
  - Update the `--exclude-fonts` flag documentation

  **Test updates** (`scripts/tests/test_sync_mod.py`):
  - Add test: `test_rsync_includes_contains_fonts` — verify `"Fonts/"` and `"Fonts/**"` in `_RSYNC_INCLUDES`
  - Add test: `test_build_rsync_command_includes_fonts` — verify `--include=Fonts/` in built command

  **Must NOT do**:
  - Change the `--exclude-fonts` behavior (it already works correctly as an override)
  - Modify `_RSYNC_EXCLUDES` (wildcard `*` correctly blocks everything not included)

  **Recommended Agent Profile**:
  - **Category**: `quick`
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1 (with Tasks 1, 2)
  - **Blocks**: Task 5
  - **Blocked By**: None

  **References**:

  **Pattern References**:
  - `scripts/sync_mod.py:14-21` — Current `_RSYNC_INCLUDES` tuple (add Fonts/ entries after Localization/**)
  - `scripts/sync_mod.py:62-87` — `build_rsync_command` function (no changes needed — includes are auto-expanded)
  - `scripts/tests/test_sync_mod.py:97-104` — Existing Bootstrap.cs include tests (follow same pattern for Fonts/)
  - `docs/deployment.md` — Deployed files table and flag documentation

  **Acceptance Criteria**:
  - [ ] `"Fonts/"` and `"Fonts/**"` in `_RSYNC_INCLUDES`
  - [ ] `python scripts/sync_mod.py --dry-run` output includes Fonts/
  - [ ] `docs/deployment.md` documents Fonts/ directory
  - [ ] `pytest scripts/tests/test_sync_mod.py` passes including new tests
  - [ ] `ruff check scripts/` passes

  **QA Scenarios**:
  ```
  Scenario: sync_mod.py deploys Fonts/
    Tool: Bash
    Steps:
      1. python3 -c "from scripts.sync_mod import _RSYNC_INCLUDES; assert 'Fonts/' in _RSYNC_INCLUDES; assert 'Fonts/**' in _RSYNC_INCLUDES; print('OK')"
      2. ruff check scripts/sync_mod.py → exit 0
      3. pytest scripts/tests/test_sync_mod.py -v → all pass
    Expected Result: Fonts/ in includes, lint clean, tests pass
    Evidence: .sisyphus/evidence/font-sync-check.txt

  Scenario: build_release includes fonts in ZIP
    Tool: Bash
    Steps:
      1. python3 -c "import zipfile; z=zipfile.ZipFile('dist/QudJP-v0.1.0.zip'); fonts=[n for n in z.namelist() if 'Fonts/' in n]; print(f'Font files in ZIP: {len(fonts)}'); assert len(fonts) >= 2"
         (run after build_release.py generates the ZIP)
    Expected Result: At least 2 font-related files in ZIP (OTF + OFL.txt)
    Evidence: .sisyphus/evidence/font-release-zip.txt
  ```

  **Commit**: YES
  - Message: `chore(scripts): add Fonts/ directory to deployment tooling`
  - Files: `scripts/sync_mod.py`, `scripts/tests/test_sync_mod.py`, `docs/deployment.md`

### Wave 2: Core Implementation

- [x] 4. Rewrite FontManager.cs — CJK Font Injection via TMP Global Fallback

  **What to do**:
  - Replace the entire content of `Mods/QudJP/Assemblies/src/FontManager.cs` (currently 19 lines, no-op) with ~60-line implementation:
  - **Conditional imports** (top of file):
    ```csharp
    #if HAS_TMP
    using TMPro;
    using UnityEngine;
    using UnityEngine.TextCore.LowLevel;
    #endif
    ```
  - **Core usings** (always available):
    ```csharp
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Threading;
    ```
  - **Initialize() method** — within `#if HAS_TMP` block:
    1. Guard with `Interlocked.Exchange` (existing pattern)
    2. Resolve font path: `Assembly.GetExecutingAssembly().Location` → parent dir → `"Fonts/NotoSansCJKjp-Regular-Subset.otf"` (reuse pattern from `LocalizationAssetResolver.ResolveModRootDirectory`)
    3. Verify file exists → throw `FileNotFoundException` if missing
    4. `Debug.Log($"[QudJP] FontManager: Loading CJK font from {fontPath}")`
    5. `TMP_FontAsset.CreateFontAsset(fontPath, 0, 96, 6, GlyphRenderMode.SDFAA, 4096, 4096)`
    6. Null check result → throw `InvalidOperationException` if null
    7. Set `fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic`
    8. Set `fontAsset.isMultiAtlasTexturesEnabled = true`
    9. `TMP_Settings.fallbackFontAssets ??= new List<TMP_FontAsset>()`
    10. `TMP_Settings.fallbackFontAssets.Add(fontAsset)` — ONE entry only
    11. Probe: `fontAsset.TryAddCharacters("日本語テスト")` → throw if false
    12. `Debug.Log("[QudJP] FontManager: CJK font registered as TMP global fallback.")`
  - **`#else` block** (CI path): `Trace.TraceInformation("[QudJP] FontManager: TMP unavailable (CI build). Font injection skipped.")`
  - **Private helper `ResolveFontPath()`**: Extract path logic into separate method for testability. Use `Assembly.GetExecutingAssembly().Location` → `Path.GetDirectoryName` twice → `Path.Combine(modRoot, "Fonts", "NotoSansCJKjp-Regular-Subset.otf")`
  - **Error handling**: Entire `#if HAS_TMP` block wrapped in try-catch. Catch logs with `Debug.LogError` and re-throws (fail-fast at init time per project convention)
  - **NO font-specific test override mechanism** — path resolution already tested via `LocalizationAssetResolver` pattern. FontManager TMP calls are inherently L3-only.

  **Must NOT do**:
  - Set `TMP_Settings.defaultFontAsset` (only add to fallback list)
  - Iterate `Resources.FindObjectsOfTypeAll<TMP_FontAsset>()` for per-asset injection
  - Add any Harmony patches in this file (no OnEnable hooks)
  - Use `System.Diagnostics.Trace` in the HAS_TMP path (use `UnityEngine.Debug` for Player.log visibility)
  - Add Bold font support
  - Add console overlay logic
  - Copy code from legacy FontManager.cs

  **Recommended Agent Profile**:
  - **Category**: `deep`
  - **Skills**: []
    - Reason: Requires understanding of TMP API, conditional compilation, and careful integration with existing init chain

  **Parallelization**:
  - **Can Run In Parallel**: NO
  - **Parallel Group**: Wave 2 (sequential after Wave 1)
  - **Blocks**: Task 5
  - **Blocked By**: Tasks 1 (font path needed), 2 (TMP references needed)

  **References**:

  **Pattern References**:
  - `Mods/QudJP/Assemblies/src/FontManager.cs` — Current 19-line no-op to be replaced entirely
  - `Mods/QudJP/Assemblies/src/LocalizationAssetResolver.cs:49-77` — `ResolveModRootDirectory()` pattern: `Assembly.GetExecutingAssembly().Location` → `Path.GetDirectoryName` → `Directory.GetParent` → mod root. FontManager follows the same path resolution but appends `"Fonts/"` instead of `"Localization/"`
  - `Mods/QudJP/Assemblies/src/QudJPMod.cs` — `Initialize()` calls `FontManager.Initialize()` as first step of init chain. The call site does NOT need changes.
  - `Mods/QudJP/Bootstrap.cs` — Uses `UnityEngine.Debug.Log/LogError` pattern for Player.log visibility (FontManager should follow same pattern in HAS_TMP path)

  **API/Type References (TMP)** — **VERIFIED via DLL reflection (MetadataLoadContext on Unity.TextMeshPro.dll)**:
  - `TMPro.TMP_FontAsset.CreateFontAsset(string fontFilePath, int faceIndex, int samplingPointSize, int atlasPadding, GlyphRenderMode renderMode, int atlasWidth, int atlasHeight)` — Overload 2 of 4. Creates font asset directly from OTF file path. **Existence confirmed by reflection on game DLL** (not in public Unity docs but present in the actual binary). Parameters: path, faceIndex=0, samplingPointSize=96, atlasPadding=6, renderMode=SDFAA, atlasWidth=4096, atlasHeight=4096. No `UnityEngine.Font` bridge needed.
  - Other CreateFontAsset overloads (for reference): (1) `string familyName, string styleName, int pointSize`, (3) `Font font`, (4) `Font font, int, int, GlyphRenderMode, int, int, AtlasPopulationMode, bool`
  - `TMPro.TMP_FontAsset.atlasPopulationMode` — Set to `AtlasPopulationMode.Dynamic` (enum value 1) for on-demand glyph rendering (essential for CJK's large glyph set)
  - `TMPro.TMP_FontAsset.isMultiAtlasTexturesEnabled` — Set to `true` to allow multiple atlas textures when glyphs exceed single atlas capacity
  - `TMPro.TMP_FontAsset.TryAddCharacters(string)` — Returns bool indicating whether characters were successfully added to the font atlas
  - `TMPro.TMP_Settings.fallbackFontAssets` — Static `List<TMP_FontAsset>`, global fallback chain. TMP checks this when primary font lacks a glyph.
  - `UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA` — Value 4165. Signed Distance Field with Anti-Aliasing render mode. Recommended for CJK text.

  **External References**:
  - Oracle's recommended minimal design (from handoff context): 6 steps, ~60-80 lines total

  **Acceptance Criteria**:
  - [ ] `Mods/QudJP/Assemblies/src/FontManager.cs` is 40-80 lines (not 19-line no-op)
  - [ ] File contains `#if HAS_TMP` conditional compilation
  - [ ] File contains `TMP_FontAsset.CreateFontAsset` call (within #if HAS_TMP)
  - [ ] File contains `TMP_Settings.fallbackFontAssets` registration (ONE Add call)
  - [ ] File contains `TryAddCharacters` probe validation
  - [ ] File uses `UnityEngine.Debug.Log` (not `System.Diagnostics.Trace`) in HAS_TMP path
  - [ ] `dotnet build Mods/QudJP/Assemblies/QudJP.csproj` succeeds with 0 warnings
  - [ ] `dotnet test Mods/QudJP/Assemblies/QudJP.Tests/QudJP.Tests.csproj` — all existing 101 tests still pass

  **QA Scenarios**:
  ```
  Scenario: FontManager.cs has correct structure
    Tool: Bash (grep)
    Steps:
      1. grep -c "#if HAS_TMP" Mods/QudJP/Assemblies/src/FontManager.cs → 1
      2. grep -c "TMP_FontAsset.CreateFontAsset" Mods/QudJP/Assemblies/src/FontManager.cs → 1
      3. grep -c "TMP_Settings.fallbackFontAssets" Mods/QudJP/Assemblies/src/FontManager.cs → ≥1
      4. grep -c "TryAddCharacters" Mods/QudJP/Assemblies/src/FontManager.cs → 1
      5. grep -c "Debug.Log\|Debug.LogError" Mods/QudJP/Assemblies/src/FontManager.cs → ≥2
      6. grep -c "Trace.Trace" Mods/QudJP/Assemblies/src/FontManager.cs → ≤1 (only in #else CI path)
      7. wc -l Mods/QudJP/Assemblies/src/FontManager.cs → 40-80
    Expected Result: All structural checks pass
    Evidence: .sisyphus/evidence/font-manager-structure.txt

  Scenario: Build and existing tests pass
    Tool: Bash
    Steps:
      1. dotnet build Mods/QudJP/Assemblies/QudJP.csproj -c Release 2>&1 | tail -3 → "Build succeeded", "0 Warning(s)", "0 Error(s)"
      2. dotnet test Mods/QudJP/Assemblies/QudJP.Tests/QudJP.Tests.csproj --no-build 2>&1 | grep "Passed\|Failed" → "101 Passed, 0 Failed" (or more)
    Expected Result: Clean build, all existing tests pass
    Failure Indicators: TreatWarningsAsErrors triggers, test count drops below 101
    Evidence: .sisyphus/evidence/font-manager-build.txt
  ```

  **Commit**: YES (groups with Task 2)
  - Message: `feat(patch): implement CJK font injection via TMP global fallback`
  - Files: `Mods/QudJP/Assemblies/QudJP.csproj`, `Mods/QudJP/Assemblies/src/FontManager.cs`

### Wave 3-4: Integration and Verification

- [x] 5. Build, Test, and Deploy

  **What to do**:
  - Run full build: `dotnet build Mods/QudJP/Assemblies/QudJP.csproj -c Release`
  - Run all C# tests: `dotnet test Mods/QudJP/Assemblies/QudJP.Tests/QudJP.Tests.csproj`
  - Run all Python tests: `pytest scripts/tests/`
  - Run linter: `ruff check scripts/`
  - Deploy to game: `python scripts/sync_mod.py`
  - Verify deployed files:
    - `~/Library/.../Mods/QudJP/Fonts/NotoSansCJKjp-Regular-Subset.otf` exists
    - `~/Library/.../Mods/QudJP/Fonts/OFL.txt` exists
    - `~/Library/.../Mods/QudJP/Assemblies/QudJP.dll` is fresh (timestamp check)
    - All existing deployed files still present (manifest.json, Bootstrap.cs, Localization/)

  **Recommended Agent Profile**:
  - **Category**: `quick`
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: NO
  - **Parallel Group**: Wave 3 (sequential after Wave 2)
  - **Blocks**: Task 6
  - **Blocked By**: Tasks 3, 4

  **Acceptance Criteria**:
  - [ ] `dotnet build` — 0 warnings, 0 errors
  - [ ] `dotnet test` — all tests pass (101+)
  - [ ] `pytest` — all tests pass (85+)
  - [ ] `ruff check` — 0 issues
  - [ ] Font file deployed to game directory
  - [ ] No .cs source files in deployed Assemblies/ directory

  **QA Scenarios**:
  ```
  Scenario: Full pipeline passes
    Tool: Bash
    Steps:
      1. dotnet build Mods/QudJP/Assemblies/QudJP.csproj -c Release → exit 0
      2. dotnet test Mods/QudJP/Assemblies/QudJP.Tests/QudJP.Tests.csproj → all pass
      3. pytest scripts/tests/ → all pass
      4. ruff check scripts/ → exit 0
      5. python scripts/sync_mod.py → exit 0
      6. test -f "$HOME/Library/Application Support/Steam/steamapps/common/Caves of Qud/CoQ.app/Contents/Resources/Data/StreamingAssets/Mods/QudJP/Fonts/NotoSansCJKjp-Regular-Subset.otf" && echo "Font deployed"
    Expected Result: All checks pass, font file deployed
    Evidence: .sisyphus/evidence/font-deploy-verify.txt
  ```

  **Commit**: NO (deployment step, not code change)

- [ ] 6. L3 Game Verification — CJK Font Rendering

  **What to do**:
  - Clear Player.log: `> ~/Library/Logs/Freehold\ Games/CavesOfQud/Player.log`
  - User launches Caves of Qud
  - Check Player.log for:
    1. `[QudJP] FontManager:` messages (font loading, registration, probe success)
    2. NO `[QudJP].*error` or `[QudJP].*exception` entries
    3. NO `Missing glyph` or `Missing character` TMP warnings
  - Visual verification in at least 3 UI areas:
    1. **Options menu** — Check menu items show Japanese text
    2. **Character creation** — Mutations, genotypes show Japanese names
    3. **In-game** — Start new game in Joppa, check: message log, NPC names, item tooltips
  - If specific screens show tofu/invisible text despite others working:
    - Document which screens fail
    - Check if those screens use non-TMP rendering (bitmap font, UI.Text)
    - Create scope for Task 7 (targeted patches)
  - If ALL screens show Japanese text: Task 7 is cancelled, proceed to main roadmap

  **Recommended Agent Profile**:
  - Manual (user action required for game launch)
  - Agent preps Player.log clear and post-launch analysis

  **Parallelization**:
  - **Can Run In Parallel**: NO
  - **Parallel Group**: Wave 4 (after Task 5)
  - **Blocks**: Task 7 (conditional), main roadmap Task 18, F3
  - **Blocked By**: Task 5

  **References**:
  - Player.log: `~/Library/Logs/Freehold Games/CavesOfQud/Player.log`
  - L3 QA common prerequisites from main roadmap (other mods disabled, log cleared)

  **Acceptance Criteria**:
  - [ ] Player.log contains `[QudJP] FontManager:` success messages (≥3 lines)
  - [ ] Player.log contains NO `[QudJP].*error` or `[QudJP].*exception`
  - [ ] Japanese text visible in Options menu
  - [ ] Japanese text visible in character creation (mutations/genotypes)
  - [ ] Japanese text visible in message log during gameplay

  **QA Scenarios**:
  ```
  Scenario: Player.log confirms font injection success
    Tool: Bash (post game-launch)
    Steps:
      1. grep -c "\[QudJP\] FontManager:" ~/Library/Logs/Freehold\ Games/CavesOfQud/Player.log → ≥3
      2. grep -ic "qudjp.*error\|qudjp.*exception\|qudjp.*failed" ~/Library/Logs/Freehold\ Games/CavesOfQud/Player.log → 0
      3. grep -ic "missing glyph\|missing character" ~/Library/Logs/Freehold\ Games/CavesOfQud/Player.log → 0 (or very few)
    Expected Result: Font loaded, registered, probe passed, no errors
    Evidence: .sisyphus/evidence/font-player-log.txt (copy of Player.log)

  Scenario: Japanese text renders in game UI (visual — user confirms)
    Tool: Game UI (manual observation)
    Preconditions: L3 QA common prerequisites (other mods disabled, log cleared)
    Steps:
      1. Launch game → main menu visible
      2. Navigate to Options → check for Japanese menu labels
      3. Start new character → check mutation names
      4. Start game in Joppa → check message log, NPC names
    Expected Result: Japanese characters visible (not tofu/invisible) in all checked areas
    Failure Indicators: □□□ tofu characters, invisible text, English-only display despite XML translations present
    Evidence: .sisyphus/evidence/font-visual-check.txt (user description or screenshot reference)
  ```

  **Commit**: NO (verification step)

### Wave 5: Conditional (only if L3 reveals issues)

- [ ] 7. Targeted Screen Patches (CONDITIONAL — only if Task 6 reveals failures)

  **What to do**:
  - **ONLY execute if Task 6 identifies specific screens where CJK text doesn't render** despite global TMP fallback being registered
  - Scope is determined by Task 6 results. Possible scenarios:
    - **Scenario A: Specific TMP components ignore global fallback** → Add targeted Harmony Postfix on those components' `Awake`/`OnEnable` to explicitly set fallback font
    - **Scenario B: Non-TMP screens (legacy UI.Text or bitmap)** → Add targeted font replacement for those specific components only
    - **Scenario C: Console overlay needed** → Implement minimal ConsoleBridge (last resort, only if DrawBuffer path proves to be non-TMP for game world rendering)
  - Each targeted patch gets:
    - Its own patch class in `src/Patches/`
    - L2 test with DummyTarget
    - Documentation of which screen it fixes
  - **Principle**: One patch per identified failure. No blanket "apply to all TMP components" patterns.

  **Must NOT do**:
  - Execute this task if Task 6 shows all screens rendering correctly
  - Add blanket OnEnable hooks for all TMP text components
  - Implement full ConsoleBridge unless DrawBuffer is confirmed non-TMP
  - Add more than the minimum patches needed to fix identified failures

  **Recommended Agent Profile**:
  - **Category**: `deep`
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: NO
  - **Parallel Group**: Wave 5 (conditional, after Task 6)
  - **Blocks**: Main roadmap Task 18, F3
  - **Blocked By**: Task 6 (results determine scope)

  **References**:
  - Task 6 L3 results (determines scope)
  - `/Users/sankenbisha/Dev/Caves-of-Qud_Japanese_legacy/Docs/font_pipeline.md:59-62` — Legacy known pitfalls (design reference only)
  - `Mods/QudJP/Assemblies/src/Patches/` — Existing patch file patterns to follow

  **Acceptance Criteria**:
  - [ ] If executed: Each identified failing screen now renders Japanese text
  - [ ] If executed: Each new patch has a corresponding L2 test
  - [ ] If executed: `dotnet build Mods/QudJP/Assemblies/QudJP.csproj` — 0 warnings
  - [ ] If executed: `dotnet test Mods/QudJP/Assemblies/QudJP.Tests/QudJP.Tests.csproj` — all pass (including new L2 tests)
  - [ ] If NOT executed (all screens work): This task is marked CANCELLED

  **QA Scenarios**:
  ```
  Scenario: Targeted patches build and pass tests (if executed)
    Tool: Bash
    Preconditions: Task 6 identified specific failing screens; patches written per scenario A/B/C
    Steps:
      1. dotnet build Mods/QudJP/Assemblies/QudJP.csproj -c Release → exit 0, 0 warnings
      2. dotnet test Mods/QudJP/Assemblies/QudJP.Tests/QudJP.Tests.csproj → all pass (101+ existing + new L2)
      3. For each new patch file: grep -c "Category(\"L2\")" Mods/QudJP/Assemblies/QudJP.Tests/*.cs → ≥1 per patch
    Expected Result: Clean build, all tests pass, each patch has L2 coverage
    Failure Indicators: TreatWarningsAsErrors fires, test count doesn't increase, missing L2 tests
    Evidence: .sisyphus/evidence/font-targeted-patches-build.txt

  Scenario: Targeted patches fix identified screens (if executed — L3 re-verification)
    Tool: Bash (post game-launch) + Game UI (manual observation)
    Preconditions: Patches deployed via sync_mod.py, game relaunched
    Steps:
      1. Clear Player.log: > ~/Library/Logs/Freehold\ Games/CavesOfQud/Player.log
      2. User launches game
      3. grep "\[QudJP\]" ~/Library/Logs/Freehold\ Games/CavesOfQud/Player.log → new patch log entries visible, no errors
      4. Navigate to each previously-failing screen identified in Task 6
      5. Verify Japanese text now renders (not tofu/invisible) on each screen
    Expected Result: All previously-failing screens now show Japanese text
    Failure Indicators: Same tofu/invisible text on identified screens, new [QudJP].*error entries
    Evidence: .sisyphus/evidence/font-targeted-patches-verify.txt

  Scenario: Task cancelled — all screens already work (if NOT executed)
    Tool: N/A
    Preconditions: Task 6 confirmed Japanese text renders on all checked screens
    Steps:
      1. Mark this task as CANCELLED in todo tracking
      2. No code changes needed
    Expected Result: Task skipped, no patches added
    Evidence: .sisyphus/evidence/font-task7-cancelled.txt (note: "All screens rendered correctly per Task 6")
  ```

  **Commit**: YES (if executed)
  - Message: `fix(patch): add targeted font patches for [specific screens]`

---

## Final Verification Wave

> After Task 6 (L3 game verification), if Japanese text is confirmed visible:
> - Mark bootstrap-fix.md Task 5 "Japanese text visible" criterion as satisfied
> - Unblock main roadmap Task 18 (フルゲームプレイテスト) and F3 (Real Manual QA)
> - Proceed with v0.1.0 release workflow

---

## Commit Strategy

- Task 1: `feat(fonts): add CJK font subset and generation script`
- Task 2+4: `feat(patch): implement CJK font injection via TMP global fallback`
- Task 3: `chore(scripts): add Fonts/ directory to deployment tooling`

---

## Success Criteria

### Verification Commands

```bash
dotnet build Mods/QudJP/Assemblies/QudJP.csproj        # Expected: Build succeeded, 0 warnings
dotnet test Mods/QudJP/Assemblies/QudJP.Tests/          # Expected: All tests pass
pytest scripts/tests/                                    # Expected: All tests pass
ruff check scripts/                                      # Expected: No issues
python scripts/sync_mod.py --dry-run                    # Expected: Fonts/ in output
```

### Player.log Expected After Fix

```
[QudJP] FontManager: Loading CJK font from .../Fonts/NotoSansCJKjp-Regular-Subset.otf
[QudJP] FontManager: CJK font asset created (Dynamic atlas, 4096x4096)
[QudJP] FontManager: Registered as TMP global fallback
[QudJP] FontManager: Probe validation passed (日本語テスト)
```

### Final Checklist

- [ ] Subset font file exists and is < 6MB
- [ ] SIL OFL license file present in Fonts/
- [ ] FontManager.cs loads font and registers TMP global fallback
- [ ] Player.log shows font loading success (no errors)
- [ ] Japanese text visible in Options menu
- [ ] Japanese text visible in Popups
- [ ] Japanese text visible in Message Log
- [ ] All existing tests still pass (101 C# + 85 Python)
- [ ] CI build succeeds (HAS_TMP not defined → no-op path)
