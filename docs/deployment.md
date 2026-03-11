# Mod Deployment Guide

How to deploy the QudJP mod to the Caves of Qud game directory.

---

## Prerequisites

- Caves of Qud installed (Steam, macOS)
- `QudJP.dll` built via `dotnet build`

---

## Deployment Methods

### Method 1: sync_mod.py (Recommended)

```bash
# Build then deploy
dotnet build Mods/QudJP/Assemblies/QudJP.csproj
python scripts/sync_mod.py
```

`sync_mod.py` uses an rsync include-first strategy to deploy only game-essential files.

**Dry run** (preview without copying):

```bash
python scripts/sync_mod.py --dry-run
```

**Exclude fonts** (faster when fonts have not changed):

```bash
python scripts/sync_mod.py --exclude-fonts
```

### Method 2: Manual Copy

```bash
GAME_MODS="$HOME/Library/Application Support/Steam/steamapps/common/Caves of Qud/CoQ.app/Contents/Resources/Data/StreamingAssets/Mods"

# Remove previous deployment
rm -rf "$GAME_MODS/QudJP"

# Copy only required files
mkdir -p "$GAME_MODS/QudJP/Assemblies"
cp Mods/QudJP/manifest.json "$GAME_MODS/QudJP/"
cp Mods/QudJP/Assemblies/QudJP.dll "$GAME_MODS/QudJP/Assemblies/"
cp -r Mods/QudJP/Localization "$GAME_MODS/QudJP/"
```

---

## Deployed Files

The game requires exactly three types of files:

| File | Purpose |
|------|---------|
| `manifest.json` | Mod metadata (ID, title, DLL path) |
| `Assemblies/QudJP.dll` | Pre-compiled Harmony patch DLL |
| `Localization/` | XML translation files + JSON dictionaries |

### Files That Must NOT Be Deployed

| File | Reason |
|------|--------|
| `*.cs` | Game's Unity/Mono compiler attempts to compile them and fails |
| `*.csproj`, `*.sln` | Build configuration files (not needed by the game) |
| `*.pdb` | Debug symbols (not needed by the game) |
| `bin/`, `obj/` | Build artifacts |
| `src/` | Source code directory |
| `QudJP.Tests/` | Test project |
| `QudJP.Analyzers/` | Roslyn analyzer project |
| `AGENTS.md` | Development documentation |

> **Critical**: The game's mod system automatically attempts to compile any `.cs` file found in the mod directory. QudJP uses a pre-compiled DLL, so if source files are present, C# 10+ syntax (`global using`, file-scoped namespaces, etc.) will cause compilation errors in the game's older compiler (CS8652, CS1514, +438 errors).

---

## Deployment Target Path (macOS Steam)

```
~/Library/Application Support/Steam/steamapps/common/
  Caves of Qud/CoQ.app/Contents/Resources/Data/StreamingAssets/Mods/QudJP/
```

---

## Post-Deployment Verification

1. Launch the game
2. Confirm **"Caves of Qud 日本語化"** appears in the Mod Manager
3. Set the mod to ENABLED
4. Restart the game and verify the Options screen displays Japanese text

### Troubleshooting

| Symptom | Cause | Fix |
|---------|-------|-----|
| FAILED + CS8652/CS1514 errors | `.cs` source files were deployed | Re-deploy with `sync_mod.py` (excludes source files) |
| Mod not listed | `manifest.json` not deployed | Verify `manifest.json` exists at the deploy target |
| Japanese text shows as □ (tofu) | CJK font not bundled | Verify Fonts directory is deployed |
| DLL load error | `QudJP.dll` not built | Run `dotnet build` then re-deploy |

---

## L3 Testing (In-Game Verification)

Manual checks that cannot be covered by automated tests (L1/L2):

- [ ] "Caves of Qud 日本語化" appears in the Mod Manager
- [ ] Options screen displays Japanese text
- [ ] Character creation screen is localized
- [ ] Japanese characters render correctly (no □ tofu)
- [ ] Player.log contains no Missing glyph / encoding errors
