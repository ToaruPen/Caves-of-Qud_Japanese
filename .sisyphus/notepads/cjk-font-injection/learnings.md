# Learnings — CJK Font Injection

## [2026-03-12] Task 2: TMP DLL References
- `HAS_TMP` define added via `<PropertyGroup Condition="Exists(...)">` + `<DefineConstants>$(DefineConstants);HAS_TMP</DefineConstants>` pattern
- 4 Unity DLL references added in a single `<ItemGroup Condition="Exists('$(ManagedDir)/Unity.TextMeshPro.dll')">` block
- All 4 references use `<Private>false</Private>` — consistent with project conventions
- Conditional `Exists()` guard ensures CI (no game DLLs) builds cleanly
- Build result: 0 Warning(s), 0 Error(s) with `TreatWarningsAsErrors=true`
- DLLs resolved at ManagedDir: Unity.TextMeshPro, UnityEngine.CoreModule, UnityEngine.TextCoreFontEngineModule, UnityEngine.TextCoreTextEngineModule

## [2026-03-12] Task 3: Deployment Tooling Update
- `_RSYNC_INCLUDES` uses include-first strategy; adding `"Fonts/"` + `"Fonts/**"` is sufficient — the existing `--exclude=*` wildcard blocks everything not explicitly included
- `--exclude-fonts` flag inserts `--exclude=Fonts/` BEFORE includes; this takes precedence in rsync and correctly overrides the `Fonts/` include — no change needed
- `create_zip` Bootstrap.cs and Fonts/ blocks use `if ... .exists()` / `if ... .is_dir()` guards so no fixture changes needed — `test_returns_member_list` still asserts `len(members) == 4` correctly (no Bootstrap.cs or Fonts/ in the test fixture)
- `Fonts/` rglob in `create_zip` uses `sorted()` for deterministic archive order; mirrors `collect_localization_files` pattern
- `relative_to(manifest_path.parent)` gives `Fonts/TestFont.otf`; prefixing `QudJP/` yields correct archive path
- Test count: 83 → 88 (+5 tests: 2 sync_mod, 1 build_release font, 2 implicit via fixture reuse)

## [2026-03-12] Task 1: Font File Preparation
- Noto Sans CJK JP v2.004 release tag: `Sans2.004` at `notofonts/noto-cjk` (note: repo moved from `googlefonts/noto-cjk` to `notofonts/noto-cjk`)
- Japanese OTF ZIP: `06_NotoSansCJKjp.zip` (94MB) at `https://github.com/notofonts/noto-cjk/releases/download/Sans2.004/06_NotoSansCJKjp.zip`
- Font file inside ZIP: `NotoSansCJKjp-Regular.otf` (nested in subdirectory; script flattens to tmp dir root)
- CJK range `U+4E00-9FFF` (20,992 chars) produces ~10.8MB subset — exceeds 6MB limit
- Narrowing to `U+4E00-6FFF` (8,192 chars) produces 4.07MB — within limit, 17,096 glyphs
- fonttools subset API: use `ft_subset.load_font()`, `Subsetter.populate(unicodes=[...])`, `save_font()` — NOT the CLI subprocess approach
- `options.layout_features = ["*"]` preserves all OpenType layout features (GSUB/GPOS)
- `urllib.request.urlretrieve` needs `# noqa: S310` for ruff S310 (audit URL open)
- `from fontTools import subset` inside function needs `# noqa: PLC0415` (import not at top level)
- ruff RET504: `parser = ...; return parser` → must be `return ...` directly
- OFL copyright holder for Noto CJK: `Copyright 2014-2021 Adobe (http://www.adobe.com/), with Reserved Font Name 'Noto'`
- `pip install --break-system-packages` needed on macOS system Python (PEP 668)

## FontManager direct TMP API call (2026-03-12)

- `TMP_FontAsset.CreateFontAsset(string, ...)` returns non-nullable `TMP_FontAsset` per its annotations
- Roslyn CA1508 ("always false") fires on `fontAsset is null` check
- Sonar S1905 fires on redundant `as TMP_FontAsset` cast workaround
- Solution: keep direct call + `#pragma warning disable CA1508` with justification comment
  - Unity runtime can return null even for non-nullable types at runtime
  - pragma suppress is the only clean option when both CA1508 and S1905 fire together
- `using UnityEngine;` resolves ambiguous `Debug` reference (no need to fully qualify)
