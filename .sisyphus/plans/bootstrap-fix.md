# Bootstrap Fix — Harmony DLL Runtime Initialization

## TL;DR

> **Quick Summary**: Fix the critical bug where Harmony DLL patches are never applied at runtime. Create a thin Bootstrap.cs loader shim (C# 9, game-compiled) that discovers and initializes the pre-built QudJP.dll via reflection. Also fix manifest.json version format and remove undocumented "Assemblies" field.
>
> **Deliverables**:
> - `Mods/QudJP/Bootstrap.cs` — Game-compiled loader shim with `[ModSensitiveCacheInit]`
> - Fixed `manifest.json` — valid version, no "Assemblies" field
> - Updated `sync_mod.py` + `docs/deployment.md` — deploy Bootstrap.cs
> - Verified Player.log showing "QudJP: Bootstrap" trace messages
>
> **Estimated Effort**: Short (1-2 hours implementation + manual game verification)
> **Parallel Execution**: YES — 2 waves
> **Critical Path**: Task 1 (Bootstrap.cs) → Task 4 (Build+Deploy) → Task 5 (Game Verify)

---

## Context

### Problem Statement

The QudJP mod's 11 Harmony patches and 36 JSON dictionary translations are never applied at runtime. Player.log confirms `QudJPMod.Init()` is never called — the game has no mechanism to discover or invoke it.

### Root Causes (Verified)

1. **`manifest.json "Version": "0.1.0-dev"`** — `XRL.Version` parser throws `System.FormatException` (MODERROR in Player.log). Wiki and all mods use `"0.1.0"` format without pre-release suffixes.

2. **No game-recognized bootstrap attributes** — `QudJPMod.Init()` and `Reset()` are plain `public static` methods. The game requires `[HasModSensitiveStaticCache]` + `[ModSensitiveCacheInit]` (namespace `XRL`) to discover and call startup code.

3. **`"Assemblies"` manifest field is undocumented** — The official wiki lists these fields: `ID, LoadOrder, Title, Description, Tags, Version, Author, PreviewImage, Dependencies, LoadBefore, LoadAfter, Directories`. No `Assemblies` field. Zero other mods use it. It may be silently ignored by the game's ModManager.

### Research Evidence

| Finding | Source | Confidence |
|---------|--------|------------|
| `[HasModSensitiveStaticCache]` in `XRL` namespace | sow-reap-scythes (2025-11), qud-xunity-autotranslator (2025-03), Wiki | HIGH |
| `[ModSensitiveCacheInit]` — `public static void`, no params | sow-reap-scythes `Init.cs` (2025-11), Wiki canonical example | HIGH |
| Fires at: main menu load + mod config change (game restarts since build 210.0) | Wiki, Steam patch notes | HIGH |
| `[HasCallAfterGameLoaded]` — 3+ production examples | Species-Manager (2026-03), quick-save-load, QudUX | HIGH |
| `Assembly.LoadFrom()` works in Unity Mono | Unity CsReference evidence | HIGH |
| Game compiles .cs with C# 9 max (no file-scoped namespaces) | Wiki (updated 2024-12-28) + community mods | HIGH |
| `"Assemblies"` field NOT in wiki docs | Wiki Mod Configuration page (updated 2025-09-19) | HIGH |
| Zero public mods use pre-built DLL + Assembly.LoadFrom | GitHub search (21 CoQ repos) | HIGH |
| Unity 6 migration (build 210.0) did NOT break mod API | Steam patch notes, post-210 mods working | HIGH |
| `ModManager.Mods` available during init callback | Logical (after mod compilation) | MEDIUM |

**Reference mod freshness** (verified 2026-03-12):
- `gnarf/qud-xunity-autotranslator` (2025-03-25) — **same type of mod (translation)**, uses `[HasModSensitiveStaticCache]` + `[ModSensitiveCacheInit]`
- `BinaryDoubts/sow-reap-scythes` (2025-11-07) — uses **exact pattern** planned for Bootstrap.cs
- `kjorteo/Species-Manager` (2026-03-05) — confirms `[HasCallAfterGameLoaded]` works post-Unity 6
- `piestyx/QudStateExtractor` (2026-03-05) — confirms `Harmony.PatchAll()` works post-Unity 6
- QudUX (2024-01-19) — **superseded as primary reference** by the above, still valid for API shape

### Why This Fix is Necessary

- XML translations (Options, Skills, Conversations) work because the game's merge system handles them natively — no DLL needed
- JSON dictionary translations (UI text, messages, mutations) require Harmony patches to intercept game text rendering at runtime
- Without Bootstrap.cs, the DLL is dead code — loaded but never initialized

---

## Work Objectives

### Core Objective

Make QudJP.dll's Harmony patches and JSON dictionary translations functional at runtime by adding a game-recognized bootstrap mechanism.

### Concrete Deliverables

- `Mods/QudJP/Bootstrap.cs` — ~40-line C# 9 loader shim
- `Mods/QudJP/manifest.json` — version fix + Assemblies removal
- `scripts/sync_mod.py` — include Bootstrap.cs in deploy
- `docs/deployment.md` — document Bootstrap.cs exception
- Player.log evidence of successful initialization

### Definition of Done

- [ ] `dotnet build Mods/QudJP/Assemblies/QudJP.csproj` succeeds (existing code unchanged)
- [ ] `dotnet test Mods/QudJP/Assemblies/QudJP.Tests/QudJP.Tests.csproj` — all 101 tests pass
- [ ] `pytest scripts/tests/` — all 83 tests pass (including new sync_mod tests)
- [ ] `python scripts/sync_mod.py --dry-run` shows Bootstrap.cs in output
- [ ] Player.log contains "QudJP: Bootstrap" trace messages after game launch
- [ ] Player.log contains NO `MODERROR` for version format
- [ ] Harmony patches are active (UI/mutation text appears in Japanese)

### Must Have

- Bootstrap.cs compiled by game's Roslyn compiler (C# ≤9 syntax only)
- `[HasModSensitiveStaticCache]` + `[ModSensitiveCacheInit]` from `XRL` namespace
- Reflection-based DLL loading (no compile-time reference to QudJP types)
- `Assembly.LoadFrom()` for DLL loading (preserves `.Location` property for path resolution)
- AppDomain assembly check before LoadFrom (handles case where DLL is already loaded)
- Fail-fast with `Trace.TraceError` on any bootstrap failure
- manifest.json `"Version"` using simple semver (`"0.1.0"`, no `-dev` suffix)
- sync_mod.py deploying Bootstrap.cs alongside manifest.json and QudJP.dll

### Must NOT Have (Guardrails)

- C# 10+ syntax in Bootstrap.cs (file-scoped namespaces, global using, etc.)
- Direct type references to `QudJP.*` namespace in Bootstrap.cs (must use reflection)
- Silent failure — every error path must log via `Trace.TraceError` before returning/throwing
- `manifest.json "Assemblies"` field (undocumented, may cause double-loading)
- Any changes to QudJPMod.cs core logic (Init/PatchAll reflection is correct as-is)
- Deployment of any .cs files OTHER than Bootstrap.cs (src/*.cs must never reach game dir)

---

## Verification Strategy

> **ZERO HUMAN INTERVENTION for automated checks** — L3 game verification requires manual game launch.

### Test Decision

- **Infrastructure exists**: YES (NUnit + pytest)
- **Automated tests**: Tests-after for sync_mod.py changes; Bootstrap.cs itself cannot be tested in CI (requires Assembly-CSharp.dll)
- **Framework**: NUnit (C#), pytest (Python)

### QA Policy

- Automated: `dotnet test` + `pytest` + `ruff check` + build verification
- Manual (L3): Game launch → Player.log inspection → visual confirmation of Japanese text
- Evidence: `.sisyphus/evidence/bootstrap-*.txt` for automated, Player.log for L3

---

## Execution Strategy

### Parallel Execution Waves

```
Wave 1 (Start Immediately — 3 parallel tasks):
├── Task 1: Create Bootstrap.cs [quick]
├── Task 2: Fix manifest.json [quick]
└── Task 3: Update sync_mod.py + deployment docs [quick]

Wave 2 (After Wave 1 — build + deploy + verify):
├── Task 4: Build, test, deploy [quick]
└── Task 5: Game verification — L3 (manual game launch) [manual]

Wave 3 (Optional cleanup):
└── Task 6: Fix HiddenMutations.jp.xml MODWARN [quick]
```

### Dependency Matrix

| Task | Depends On | Blocks | Wave |
|------|-----------|--------|------|
| 1 | — | 4 | 1 |
| 2 | — | 4 | 1 |
| 3 | — | 4 | 1 |
| 4 | 1, 2, 3 | 5 | 2 |
| 5 | 4 | — | 2 |
| 6 | — | — | 3 (independent) |

### Agent Dispatch Summary

- **Wave 1**: 3 tasks → T1 `quick`, T2 `quick`, T3 `quick`
- **Wave 2**: 2 tasks → T4 `quick`, T5 manual (user)
- **Wave 3**: 1 task → T6 `quick`

---

## TODOs

- [x] 1. Create `Mods/QudJP/Bootstrap.cs` — Game-Compiled Loader Shim

  **What to do**:
  - Create `Mods/QudJP/Bootstrap.cs` (~40 lines) with C# ≤9 syntax (braced `namespace QudJP { }`, NOT file-scoped)
  - Class: `[HasModSensitiveStaticCache] public static class QudJPLoader`
  - Method: `[ModSensitiveCacheInit] public static void Bootstrap()`
  - Import `using XRL;` for the attribute resolution
  - Import `using System;`, `using System.Diagnostics;`, `using System.IO;`, `using System.Reflection;`
  - Implementation flow:
    1. Log: `Trace.TraceInformation("[QudJP] Bootstrap: resolving QudJP.dll path...");`
    2. Find mod path: iterate `ModManager.Mods` to find mod with `ID == "QudJP"`, get `.Path`
    3. If mod not found → `Trace.TraceError("[QudJP] Bootstrap: mod 'QudJP' not found in ModManager.Mods")` + `throw`
    4. Build DLL path: `Path.Combine(modPath, "Assemblies", "QudJP.dll")`
    5. Verify file exists: `File.Exists(dllPath)` — if not → `Trace.TraceError` + `throw`
    6. Check if already loaded: iterate `AppDomain.CurrentDomain.GetAssemblies()` for `asm.GetName().Name == "QudJP"`
    7. If not loaded → `Assembly.LoadFrom(dllPath)` — log the path
    8. Get `QudJP.QudJPMod` type via `loadedAssembly.GetType("QudJP.QudJPMod")`
    9. Get `Init` method: `modType.GetMethod("Init", BindingFlags.Public | BindingFlags.Static)`
    10. Invoke: `initMethod.Invoke(null, null)`
    11. Log: `Trace.TraceInformation("[QudJP] Bootstrap: initialization complete.");`
  - Wrap entire body in `try { ... } catch (Exception ex) { Trace.TraceError("[QudJP] Bootstrap failed: " + ex); throw; }`
  - Fail-fast: THROW on any error (this is init time, not runtime patch)

  **Must NOT do**:
  - Use file-scoped namespace `namespace QudJP;` (C# 10 — game compiler rejects)
  - Use `global using` (C# 10)
  - Reference any type from `QudJP` namespace directly (use reflection only)
  - Silently return on error without logging AND throwing
  - Add any `[HarmonyPatch]` attributes to this file

  **Recommended Agent Profile**:
  - **Category**: `quick`
  - **Skills**: []
    - No specialized skills needed — this is a small, focused file creation

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1 (with Tasks 2, 3)
  - **Blocks**: Task 4
  - **Blocked By**: None

  **References**:

  **Pattern References** (how to structure the loader):
  - `Mods/QudJP/Assemblies/src/QudJPMod.cs:12-15` — `Init()` is the entry point to call via reflection. It's `public static void Init()` → calls `Initialize()` → `FontManager.Initialize()` + `ApplyHarmonyPatches()`
  - `Mods/QudJP/Assemblies/src/QudJPMod.cs:22-27` — `Interlocked.Exchange` ensures idempotent init — safe to call from Bootstrap even if somehow called twice
  - `Mods/QudJP/Assemblies/src/LocalizationAssetResolver.cs:49-77` — Uses `Assembly.GetExecutingAssembly().Location` for path resolution. `Assembly.LoadFrom()` correctly sets `.Location`, so this will work

  **Game API References** (attributes and ModManager):
  - `XRL.HasModSensitiveStaticCache` — class-level attribute, namespace `XRL` (evidence: sow-reap-scythes `Init.cs`, wiki canonical example)
  - `XRL.ModSensitiveCacheInit` — method-level attribute, namespace `XRL` (evidence: sow-reap-scythes `Init.cs` uses both attributes together, confirmed working post-Unity 6)
  - `XRL.ModManager.Mods` — collection of mod info objects, each has `.ID` (string) and `.Path` (string)
  - Wiki: https://wiki.cavesofqud.com/wiki/Modding:Adding_Code_at_Startup — canonical code example (last edited 2024-07-24)

  **Production Examples** (real mods using related attributes — all confirmed working post-Unity 6 migration):
  - sow-reap-scythes `Init.cs` (2025-11-07) — uses `[HasModSensitiveStaticCache]` + `[ModSensitiveCacheInit]` **exactly our pattern**, confirmed working on current game version
  - qud-xunity-autotranslator (2025-03-25) — translation mod using `[ModSensitiveCacheInit]` for early DLL bootstrap, same use case as QudJP
  - Species-Manager `Kjorteo_SpeciesManager.cs` (2026-03-05) — uses `[HasCallAfterGameLoadedAttribute]` with `using XRL;` (Fallback A reference)
  - QudStateExtractor (2026-03-05) — uses `Harmony.PatchAll()` at runtime, confirms Harmony still works post-Unity 6

  **Acceptance Criteria**:
  - [ ] `Mods/QudJP/Bootstrap.cs` exists and is well-formed
  - [ ] File uses ONLY C# ≤9 syntax (no file-scoped namespace, no global using)
  - [ ] File has `using XRL;` and `[HasModSensitiveStaticCache]` + `[ModSensitiveCacheInit]`
  - [ ] File uses reflection exclusively for QudJP.dll interaction (zero compile-time references)
  - [ ] File has `Assembly.LoadFrom()` with `AppDomain.GetAssemblies()` pre-check
  - [ ] All error paths log via `Trace.TraceError` and throw (fail-fast)

  **QA Scenarios**:
  ```
  Scenario: Bootstrap.cs syntax is C# 9 compatible
    Tool: Bash (grep)
    Steps:
      1. grep -c "^namespace.*;" Mods/QudJP/Bootstrap.cs → expect 0 (no file-scoped namespace)
      2. grep -c "global using" Mods/QudJP/Bootstrap.cs → expect 0
      3. grep -c "HasModSensitiveStaticCache" Mods/QudJP/Bootstrap.cs → expect 1
      4. grep -c "ModSensitiveCacheInit" Mods/QudJP/Bootstrap.cs → expect 1
      5. grep -c "using XRL" Mods/QudJP/Bootstrap.cs → expect ≥1
      6. grep -c "Assembly.LoadFrom" Mods/QudJP/Bootstrap.cs → expect 1
      7. grep -c "Trace.TraceError" Mods/QudJP/Bootstrap.cs → expect ≥2
    Expected Result: All counts match
    Evidence: .sisyphus/evidence/bootstrap-syntax-check.txt

  Scenario: Bootstrap.cs uses reflection only (no direct QudJP type refs)
    Tool: Bash (grep)
    Steps:
      1. grep -c "QudJPMod\." Mods/QudJP/Bootstrap.cs → expect 0 (no direct type reference)
      2. grep -c "GetType.*QudJP" Mods/QudJP/Bootstrap.cs → expect 1 (reflection usage)
      3. grep -c "GetMethod.*Init" Mods/QudJP/Bootstrap.cs → expect 1
      4. grep -c "\.Invoke(" Mods/QudJP/Bootstrap.cs → expect 1
    Expected Result: Zero direct refs, reflection pattern present
    Evidence: .sisyphus/evidence/bootstrap-reflection-check.txt
  ```

  **Commit**: YES (groups with Task 2)
  - Message: `fix(patch): add Bootstrap.cs loader and fix manifest.json version`
  - Files: `Mods/QudJP/Bootstrap.cs`, `Mods/QudJP/manifest.json`

- [x] 2. Fix `manifest.json` — Version Format and Assemblies Removal

  **What to do**:
  - Change `"Version": "0.1.0-dev"` → `"Version": "0.1.0"` (line 6)
  - Remove `"Assemblies": ["Assemblies/QudJP.dll"]` (line 9) — undocumented field, may cause double-loading
  - Remove trailing comma on line 8 if needed after Assemblies removal (ensure valid JSON)
  - Verify JSON is valid after changes

  **Must NOT do**:
  - Keep `-dev` or any pre-release suffix in Version
  - Keep the `Assemblies` field (Bootstrap.cs handles DLL loading)
  - Change any other fields (ID, Title, Description, etc.)

  **Recommended Agent Profile**:
  - **Category**: `quick`
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1 (with Tasks 1, 3)
  - **Blocks**: Task 4
  - **Blocked By**: None

  **References**:
  - `Mods/QudJP/manifest.json:6` — current Version value `"0.1.0-dev"`
  - `Mods/QudJP/manifest.json:9` — current Assemblies field `["Assemblies/QudJP.dll"]`
  - Wiki Mod Configuration: official fields are ID, LoadOrder, Title, Description, Tags, Version, Author, PreviewImage, Dependencies, LoadBefore, LoadAfter, Directories — NO Assemblies field

  **Acceptance Criteria**:
  - [ ] `"Version": "0.1.0"` in manifest.json
  - [ ] No `"Assemblies"` key in manifest.json
  - [ ] `python3 -c "import json; json.load(open('Mods/QudJP/manifest.json'))"` succeeds (valid JSON)

  **QA Scenarios**:
  ```
  Scenario: manifest.json is valid and correctly updated
    Tool: Bash
    Steps:
      1. python3 -c "import json; d=json.load(open('Mods/QudJP/manifest.json')); print(d['Version'])" → expect "0.1.0"
      2. python3 -c "import json; d=json.load(open('Mods/QudJP/manifest.json')); assert 'Assemblies' not in d; print('OK')" → expect "OK"
      3. python3 -c "import json; json.load(open('Mods/QudJP/manifest.json')); print('Valid JSON')" → expect "Valid JSON"
    Expected Result: Version is "0.1.0", no Assemblies field, valid JSON
    Evidence: .sisyphus/evidence/bootstrap-manifest-check.txt
  ```

  **Commit**: YES (groups with Task 1)
  - Message: `fix(patch): add Bootstrap.cs loader and fix manifest.json version`
  - Files: `Mods/QudJP/manifest.json`

- [x] 3. Update `sync_mod.py` and `docs/deployment.md`

  **What to do**:

  **sync_mod.py** (`scripts/sync_mod.py:12-18`):
  - Add `"Bootstrap.cs"` to `_RSYNC_INCLUDES` tuple, right after `"manifest.json"`
  - Updated tuple should be:
    ```python
    _RSYNC_INCLUDES: tuple[str, ...] = (
        "manifest.json",
        "Bootstrap.cs",
        "Assemblies/",
        "Assemblies/QudJP.dll",
        "Localization/",
        "Localization/**",
    )
    ```
  - Update the comment block (lines 8-11) to explain Bootstrap.cs is the ONE intentional .cs exception

  **docs/deployment.md**:
  - Add `Bootstrap.cs` to the "Deployed Files" table (line 59-63): `Bootstrap.cs | Game-compiled loader shim — initializes QudJP.dll`
  - Update the "Files That Must NOT Be Deployed" section (line 69): clarify that `Bootstrap.cs` is the exception to the "no .cs files" rule
  - Add troubleshooting entry: `No QudJP traces in Player.log | Bootstrap.cs not deployed or not compiled | Verify Bootstrap.cs exists in game Mods/QudJP/ directory`

  **sync_mod.py tests** (`scripts/tests/test_sync_mod.py`):
  - Add test: `test_rsync_includes_contains_bootstrap_cs` — verify `"Bootstrap.cs"` is in `_RSYNC_INCLUDES`
  - Add test: `test_build_rsync_command_includes_bootstrap` — verify `--include=Bootstrap.cs` appears in built command

  **Must NOT do**:
  - Deploy any .cs files from `Assemblies/src/` (only `Bootstrap.cs` from mod root)
  - Change the exclude-all pattern `("*",)` in `_RSYNC_EXCLUDES`

  **Recommended Agent Profile**:
  - **Category**: `quick`
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1 (with Tasks 1, 2)
  - **Blocks**: Task 4
  - **Blocked By**: None

  **References**:
  - `scripts/sync_mod.py:8-20` — current include/exclude lists and comment explaining .cs exclusion rationale
  - `docs/deployment.md:57-78` — deployed files table and "Must NOT deploy" section
  - `scripts/tests/test_sync_mod.py` — existing test patterns for sync_mod

  **Acceptance Criteria**:
  - [ ] `_RSYNC_INCLUDES` contains `"Bootstrap.cs"` in sync_mod.py
  - [ ] `python scripts/sync_mod.py --dry-run` output includes Bootstrap.cs
  - [ ] `docs/deployment.md` documents Bootstrap.cs as deployed file
  - [ ] pytest test verifies Bootstrap.cs is in includes
  - [ ] `ruff check scripts/` passes

  **QA Scenarios**:
  ```
  Scenario: sync_mod.py deploys Bootstrap.cs
    Tool: Bash
    Steps:
      1. python3 -c "from scripts.sync_mod import _RSYNC_INCLUDES; assert 'Bootstrap.cs' in _RSYNC_INCLUDES; print('OK')"
      2. python scripts/sync_mod.py --dry-run 2>&1 | grep -c "Bootstrap.cs" → expect ≥1
      3. ruff check scripts/sync_mod.py → expect 0 errors
      4. pytest scripts/tests/test_sync_mod.py -v → expect all pass
    Expected Result: Bootstrap.cs in includes, in dry-run output, lint clean, tests pass
    Evidence: .sisyphus/evidence/bootstrap-sync-check.txt

  Scenario: deployment docs updated
    Tool: Bash (grep)
    Steps:
      1. grep -c "Bootstrap.cs" docs/deployment.md → expect ≥2 (deployed files table + exception note)
    Expected Result: Bootstrap.cs documented
    Evidence: .sisyphus/evidence/bootstrap-docs-check.txt
  ```

  **Commit**: YES
  - Message: `chore(scripts): deploy Bootstrap.cs via sync_mod.py and update deployment docs`
  - Files: `scripts/sync_mod.py`, `scripts/tests/test_sync_mod.py`, `docs/deployment.md`

- [x] 4. Build, Test, and Deploy

  **What to do**:
  - Run `dotnet build Mods/QudJP/Assemblies/QudJP.csproj` — verify existing DLL still builds cleanly
  - Run `dotnet test Mods/QudJP/Assemblies/QudJP.Tests/QudJP.Tests.csproj` — all 101 C# tests pass
  - Run `pytest scripts/tests/` — all Python tests pass (83+)
  - Run `ruff check scripts/` — zero lint errors
  - Run `python scripts/sync_mod.py` — deploy to game Mods directory
  - Verify deployed files:
    - `~/Library/.../Mods/QudJP/Bootstrap.cs` exists
    - `~/Library/.../Mods/QudJP/manifest.json` has Version "0.1.0" and no Assemblies
    - `~/Library/.../Mods/QudJP/Assemblies/QudJP.dll` exists
    - `~/Library/.../Mods/QudJP/Localization/` contains XML + JSON files
    - NO `.cs` files in `Assemblies/` subdirectory at deploy target

  **Must NOT do**:
  - Skip any test suite (all must pass before game launch)
  - Deploy without building first (stale DLL risk)
  - Manually copy files instead of using sync_mod.py

  **Recommended Agent Profile**:
  - **Category**: `quick`
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: NO
  - **Parallel Group**: Wave 2 (sequential after Wave 1)
  - **Blocks**: Task 5
  - **Blocked By**: Tasks 1, 2, 3

  **References**:
  - `Mods/QudJP/Assemblies/QudJP.csproj` — build configuration
  - `scripts/sync_mod.py` — deployment script (updated in Task 3)
  - `docs/deployment.md` — deployment verification checklist

  **Acceptance Criteria**:
  - [ ] `dotnet build` succeeds with 0 warnings
  - [ ] `dotnet test` — 101 tests pass
  - [ ] `pytest` — 83+ tests pass
  - [ ] `ruff check` — 0 errors
  - [ ] Bootstrap.cs deployed to game directory
  - [ ] No stale .cs source files in deployed Assemblies/ directory

  **QA Scenarios**:
  ```
  Scenario: Full build + test + deploy pipeline
    Tool: Bash
    Steps:
      1. dotnet build Mods/QudJP/Assemblies/QudJP.csproj -c Release → exit 0
      2. dotnet test Mods/QudJP/Assemblies/QudJP.Tests/QudJP.Tests.csproj → "101 Passed"
      3. pytest scripts/tests/ → "83 passed" (or more)
      4. ruff check scripts/ → exit 0
      5. python scripts/sync_mod.py → exit 0
      6. ls "$HOME/Library/Application Support/Steam/steamapps/common/Caves of Qud/CoQ.app/Contents/Resources/Data/StreamingAssets/Mods/QudJP/Bootstrap.cs" → file exists
      7. find "$HOME/Library/Application Support/Steam/steamapps/common/Caves of Qud/CoQ.app/Contents/Resources/Data/StreamingAssets/Mods/QudJP/Assemblies/" -name "*.cs" | wc -l → expect 0
    Expected Result: All checks pass, Bootstrap.cs deployed, no source .cs in Assemblies/
    Evidence: .sisyphus/evidence/bootstrap-deploy-verify.txt
  ```

  **Commit**: NO (deployment step, not code change)

- [ ] 5. Game Verification — L3 Manual Test

  **What to do**:
  - Clear Player.log: `> ~/Library/Logs/Freehold\ Games/CavesOfQud/Player.log`
  - User launches Caves of Qud
  - Check Player.log for:
    1. NO `MODERROR` for version format (confirms manifest.json fix)
    2. `[QudJP] Bootstrap:` trace messages (confirms Bootstrap.cs compiled and executed)
    3. QudJP Harmony patch trace messages (confirms patches applied)
    4. No `QudJP.*error` or `QudJP.*exception` entries
  - Visual check: Options menu, mutations, UI text should show Japanese
  - If NO QudJP traces found → Execute Fallback A:
    - Change `[HasModSensitiveStaticCache]` → `[HasCallAfterGameLoaded]`
    - Change `[ModSensitiveCacheInit]` → `[CallAfterGameLoaded]`
    - Redeploy and retest
  - If Fallback A also fails → Escalate to Fallback B (script mod conversion — separate plan needed)

  **Recommended Agent Profile**:
  - Manual (user action required for game launch)
  - Agent can prep Player.log clear and post-launch analysis

  **Parallelization**:
  - **Can Run In Parallel**: NO
  - **Parallel Group**: Wave 2 (after Task 4)
  - **Blocks**: Main roadmap Task 18 + F3
  - **Blocked By**: Task 4

  **References**:
  - Player.log: `~/Library/Logs/Freehold Games/CavesOfQud/Player.log`
  - `Mods/QudJP/Bootstrap.cs` — trace messages to look for
  - Fallback A: change 2 attributes in Bootstrap.cs (see Fallback Strategy section)

  **Acceptance Criteria**:
  - [ ] Player.log contains `[QudJP] Bootstrap:` messages
  - [ ] Player.log contains NO `MODERROR` for QudJP version
  - [ ] Player.log contains NO `QudJP.*error` (case-insensitive)
  - [ ] Japanese text visible in at least one UI element (Options, mutations, or messages)

  **QA Scenarios**:
  ```
  Scenario: Player.log confirms successful bootstrap
    Tool: Bash (post game-launch)
    Steps:
      1. grep -c "QudJP.*Bootstrap" ~/Library/Logs/Freehold\ Games/CavesOfQud/Player.log → expect ≥3
      2. grep -c "MODERROR.*QudJP\|MODERROR.*日本語" ~/Library/Logs/Freehold\ Games/CavesOfQud/Player.log → expect 0
      3. grep -ic "qudjp.*error\|qudjp.*exception\|qudjp.*failed" ~/Library/Logs/Freehold\ Games/CavesOfQud/Player.log → expect 0
      4. grep -c "QudJP.*complete\|QudJP.*initialized" ~/Library/Logs/Freehold\ Games/CavesOfQud/Player.log → expect ≥1
    Expected Result: Bootstrap traces present, zero errors, initialization complete
    Evidence: .sisyphus/evidence/bootstrap-player-log.txt (copy of Player.log after test)

  Scenario: Fallback A — switch to [CallAfterGameLoaded] if needed
    Tool: Manual edit + Bash
    Preconditions: Primary [ModSensitiveCacheInit] produced zero QudJP traces
    Steps:
      1. In Bootstrap.cs: change [HasModSensitiveStaticCache] → [HasCallAfterGameLoaded]
      2. In Bootstrap.cs: change [ModSensitiveCacheInit] → [CallAfterGameLoaded]
      3. python scripts/sync_mod.py → redeploy
      4. Clear Player.log → relaunch game → start new game
      5. grep "QudJP.*Bootstrap" Player.log → expect ≥1
    Expected Result: Bootstrap runs after game load
    Evidence: .sisyphus/evidence/bootstrap-fallback-a.txt
  ```

  **Commit**: NO (verification step)

- [x] 6. Fix `HiddenMutations.jp.xml` MODWARN (Optional Cleanup)

  **What to do**:
  - Remove `BearerDescription` attributes from all `<mutation>` elements in `Mods/QudJP/Localization/HiddenMutations.jp.xml`
  - These attributes are not recognized by game version 2.0.4 and cause 50+ MODWARN log entries
  - Preserve all other attributes (Name, Description, etc.) and translation text
  - Verify XML is still well-formed after changes

  **Must NOT do**:
  - Remove any attributes other than `BearerDescription`
  - Change translation text content
  - Modify any other XML files in this task

  **Recommended Agent Profile**:
  - **Category**: `quick`
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES (independent of all other tasks)
  - **Parallel Group**: Wave 3 (or anytime)
  - **Blocks**: None
  - **Blocked By**: None

  **References**:
  - `Mods/QudJP/Localization/HiddenMutations.jp.xml` — contains `BearerDescription` attributes
  - `.sisyphus/notepads/coq-jp-roadmap/issues.md:18-29` — MODWARN investigation notes
  - Player.log MODWARN entries: `MODWARN [Caves of Qud 日本語化] - Unexpected attribute BearerDescription`

  **Acceptance Criteria**:
  - [ ] Zero `BearerDescription` attributes in HiddenMutations.jp.xml
  - [ ] `python3 -c "import xml.etree.ElementTree as ET; ET.parse('Mods/QudJP/Localization/HiddenMutations.jp.xml')"` succeeds
  - [ ] `python scripts/validate_xml.py Mods/QudJP/Localization/HiddenMutations.jp.xml` passes

  **QA Scenarios**:
  ```
  Scenario: BearerDescription attributes removed
    Tool: Bash (grep)
    Steps:
      1. grep -c "BearerDescription" Mods/QudJP/Localization/HiddenMutations.jp.xml → expect 0
      2. python3 -c "import xml.etree.ElementTree as ET; ET.parse('Mods/QudJP/Localization/HiddenMutations.jp.xml'); print('Valid XML')" → expect "Valid XML"
    Expected Result: Zero BearerDescription, valid XML
    Evidence: .sisyphus/evidence/bootstrap-modwarn-fix.txt
  ```

  **Commit**: YES
  - Message: `fix(xml): remove unsupported BearerDescription attributes from HiddenMutations`
  - Files: `Mods/QudJP/Localization/HiddenMutations.jp.xml`

---

## Final Verification Wave

> After Task 5 (game verification), if Harmony patches are confirmed active, the main roadmap's Task 18 (Full gameplay test) and F3 (Real Manual QA) can proceed.

---

## Commit Strategy

- Task 1+2: `fix(patch): add Bootstrap.cs loader and fix manifest.json version`
- Task 3: `chore(scripts): deploy Bootstrap.cs via sync_mod.py`
- Task 6: `fix(xml): remove unsupported BearerDescription attributes from HiddenMutations`

---

## Success Criteria

### Verification Commands

```bash
dotnet build Mods/QudJP/Assemblies/QudJP.csproj           # Expected: Build succeeded
dotnet test Mods/QudJP/Assemblies/QudJP.Tests/             # Expected: 101 tests pass
pytest scripts/tests/                                       # Expected: 83+ tests pass
ruff check scripts/                                         # Expected: No issues
python scripts/sync_mod.py --dry-run                       # Expected: Bootstrap.cs in output
```

### Player.log Expected After Fix

```
[QudJP] Bootstrap: resolving QudJP.dll path...
[QudJP] Bootstrap: loading assembly from <path>/Assemblies/QudJP.dll
[QudJP] Bootstrap: invoking QudJPMod.Init()...
[QudJP] Bootstrap: initialization complete.
```

### Final Checklist

- [ ] Bootstrap.cs exists and uses C# ≤9 syntax
- [ ] manifest.json Version is "0.1.0" (no -dev)
- [ ] manifest.json has no "Assemblies" field
- [ ] sync_mod.py deploys Bootstrap.cs
- [ ] All existing tests pass (101 C# + 83 Python)
- [ ] Player.log shows QudJP bootstrap trace messages
- [ ] Player.log has no MODERROR for version format
- [ ] Harmony patches are active (Japanese text in UI/mutations)

---

## Fallback Strategy

If `[ModSensitiveCacheInit]` does not work at runtime (confirmed working in sow-reap-scythes 2025-11 and qud-xunity-autotranslator 2025-03, but included as contingency):

**Fallback A**: Switch to `[HasCallAfterGameLoaded]` + `[CallAfterGameLoaded]`
- 2 production examples (Species-Manager, quick-save-load mods)
- Fires after game load (not at startup)
- Menu translations may not work until first game start
- Harmony patches apply to all subsequent gameplay

**Fallback B**: Move all Harmony patch code to .cs script files
- Proven pattern used by ALL existing CoQ mods
- Game auto-compiles and auto-applies PatchAll
- Requires rewriting patches in C# 9 syntax + removing file-scoped namespaces
- Largest change scope — last resort

**Decision Point**: After Task 5, if Player.log shows no QudJP traces, try Fallback A first (1-line attribute change). If that fails, escalate to Fallback B.
