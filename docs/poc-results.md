# Task 0 PoC Results: NUnit + HarmonyLib on macOS

## Environment

- Date: 2026-03-11 (local)
- OS: macOS 26.3.1 (Darwin 25.3.0, arm64)
- .NET SDK: 10.0.100
- Installed runtimes: Microsoft.NETCore.App 10.0.0, Microsoft.AspNetCore.App 10.0.0
- Game app version (Info.plist): CFBundleShortVersionString `2.0.4`, CFBundleVersion `0`
- Game managed assembly path:
  - `~/Library/Application Support/Steam/steamapps/common/Caves of Qud/CoQ.app/Contents/Resources/Data/Managed/Assembly-CSharp.dll`
  - `~/Library/Application Support/Steam/steamapps/common/Caves of Qud/CoQ.app/Contents/Resources/Data/Managed/0Harmony.dll`

## Part A Results (NUnit + Harmony + Assembly Reference)

PoC project was created at:

- `/tmp/qudjp-poc/QudJP.PoC.Tests`

### A-1. Minimal NUnit test execution on macOS

- Result: **PASS** (`dotnet test -f net10.0` exits 0)
- Evidence: `.sisyphus/evidence/task-0-poc-nunit.txt`

### A-2. Harmony Prefix/Postfix patching (DummyTarget)

- Implemented in `UnitTest1.cs` inside PoC project
- Prefix test: overrides return value to `patched: <input>` and skips original
- Postfix test: appends ` [postfixed]` to original output
- Result: **PASS** on `net10.0`

### A-3. Assembly-CSharp.dll reference and pure type signature access

- Added explicit reference to `Assembly-CSharp.dll` in PoC csproj
- Loaded target assembly and enumerated `Markup` type signatures
- Observed type: `ConsoleLib.Console.Markup`
- Signatures confirmed readable (`Parse`, `Transform`, `Strip`, `Wrap`, `Color`, etc.)
- Result: **PASS**
- Evidence: `.sisyphus/evidence/task-0-poc-assembly.txt`

### A-4. Game-bundled Harmony check (`0Harmony.dll`)

- Assembly identity read successfully
- Observed version: `0Harmony, Version=2.2.2.0`
- Result: **PASS**
- Evidence: `.sisyphus/evidence/task-0-poc-assembly.txt`

### A-5. TypeInitializationException behavior for Unity-dependent instantiation

- Tested multiple Unity-dependent classes from `Assembly-CSharp.dll` by running class constructors and creating instances
- Observed behavior in this environment: all sampled types instantiated successfully
- `TypeInitializationException observed: False`
- Result: **BEHAVIOR CAPTURED** (no TypeInitializationException reproduced in this specific test path)
- Evidence: `.sisyphus/evidence/task-0-poc-assembly.txt`

## Target Framework Analysis

PoC test project was configured with:

- `net8.0;net9.0;net10.0;net48;netstandard2.0`

### Test/Build matrix

- `net10.0`
  - `dotnet test`: **PASS**
  - `dotnet build`: **PASS**
- `net9.0`
  - `dotnet build`: **PASS**
  - `dotnet test`: **FAIL** (runtime missing: `Microsoft.NETCore.App 9.0.0` not installed)
- `net8.0`
  - `dotnet build`: **PASS**
  - `dotnet test`: **FAIL** (runtime missing: `Microsoft.NETCore.App 8.0.0` not installed)
- `net48`
  - `dotnet build`: **PASS** (with assembly conflict warnings)
  - `dotnet test`: **FAIL** (`mono` host not found on this macOS environment)
- `netstandard2.0`
  - `dotnet build`: **PASS** (large volume of reference/conflict warnings)
  - `dotnet test`: exits without practical test execution output in this setup

### Working framework conclusion

- **Toolchain testing framework (local macOS): `net10.0` is the only fully working test-execution target in this environment.**
- `net48` is buildable and remains relevant for game runtime compatibility, but local `dotnet test` requires `mono`.

## Part B Results (In-game Harmony runtime PoC)

### B-1. Minimal mod created

- Build project: `/tmp/qudjp-poc/QudJP.PoC.Mod`
- Target: `net48`
- DLL behavior: module initializer that only calls `new Harmony("qudjp.poc.runtime").PatchAll();`
- Build output: `/tmp/qudjp-poc/QudJP.PoC.Mod/bin/Release/net48/QudJP.PoC.Mod.dll`

### B-2. Deployment to game Mods directory

- Created Mods root:
  - `~/Library/Application Support/Steam/steamapps/common/Caves of Qud/CoQ.app/Contents/Resources/Data/StreamingAssets/Mods/`
- Deployed PoC mod:
  - `~/Library/Application Support/Steam/steamapps/common/Caves of Qud/CoQ.app/Contents/Resources/Data/StreamingAssets/Mods/QudJP_PoC/manifest.json`
  - `~/Library/Application Support/Steam/steamapps/common/Caves of Qud/CoQ.app/Contents/Resources/Data/StreamingAssets/Mods/QudJP_PoC/Assemblies/QudJP.PoC.Mod.dll`

### B-3. Player.log analysis scripts prepared

- `.sisyphus/evidence/task-0-analyze-player-log.py`
- `.sisyphus/evidence/task-0-analyze-player-log.sh`

Default analyzed log path:

- `~/Library/Logs/Freehold Games/CavesOfQud/Player.log`

## Manual verification instructions (user action required)

1. Launch **Caves of Qud**.
2. Exit the game after main menu/world load completes.
3. Provide updated log content from:
   - `~/Library/Logs/Freehold Games/CavesOfQud/Player.log`
4. Optional local pre-check command:
   - `.sisyphus/evidence/task-0-analyze-player-log.sh`

## Risks and Constraints

- Existing Player.log already contains Harmony runtime failure from another mod:
  - `mprotect returned EACCES`
  - This confirms macOS runtime patching risk is real.
- Current environment lacks:
  - .NET 8/9 runtime packs (for executing net8/net9 tests)
  - Mono host (for executing net48 tests with `dotnet test`)
- Large reference conflict warnings occur when mixing modern SDK/test tooling with Unity/Mono-targeted assemblies.

## Recommendation for subsequent tasks

1. Use `net10.0` NUnit project for local static/tooling validation (fast feedback loop).
2. Keep runtime mod DLL target on `net48` for Unity/Mono game compatibility.
3. Treat Part B game-launch log verification as mandatory gate before starting Task 1.
4. If `mprotect returned EACCES` appears with `QudJP_PoC`, prioritize fallback strategy planning (XML-only or reduced Harmony surface) before broader implementation.
