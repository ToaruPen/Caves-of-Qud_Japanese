# Decisions

## 2026-03-11 Task 0 PoC
- ローカルPoC検証の一次ターゲットは `net10.0`（実行可能性を優先）。
- ゲーム投入DLLは Unity/Mono 互換を優先して `net48` でビルド。
- Part B は手動ゲーム起動 + Player.log 検証をゲート条件として継続採用。

## 2026-03-11 Task 0: Target Framework Decision
- Test project: net10.0 (primary runner, dotnet SDK 10.0.100)
- Mod DLL: net48 (matches game's Mono runtime)
- Assembly-CSharp.dll reference: `<Private>false</Private>` (reference-only, not copied)
- Harmony: NuGet Lib.Harmony 2.4.2 for tests, game-bundled 0Harmony 2.2.2.0 for runtime
- 3-Layer test strategy: CONFIRMED viable after Part A success

## 2026-03-11 Task 1 スキャフォールディング（再実行）
- `.sln` ファイルは手動作成（dotnet CLI が `.slnx` に変換する問題を回避）。
- `.editorconfig` の C# スタイルルールは severity=error で統一（`dotnet_style_qualification` 系）。
- CI は `hashFiles()` ガードで条件付き実行（テストプロジェクト・Pythonファイル未存在時にスキップ）。
- pyproject.toml の Ruff ignore は `D100`, `D104`, `COM812`, `ISC001` の4ルールのみ。

## 2026-03-13 Task 18-20 Harmony runtime / Rosetta decision
- Apple Silicon ネイティブ ARM64 + Unity Mono + game-bundled `0Harmony 2.2.2.0` では、Harmony patch 適用が広範囲に失敗する前提で扱う。
- macOS Apple Silicon 上の実ゲーム確認は、以後 `scripts/launch_rosetta.sh` または `Launch CavesOfQud (Rosetta).command` を使った Rosetta 起動を標準手順とする。
- 根拠: ネイティブ起動では `HarmonySharedState` / `mprotect returned EACCES` 系の障害が出た一方、Rosetta 起動では `Harmony patching complete: 22 method(s) patched.` まで進んだ。
- したがって、Apple Silicon 上で native ARM64 ログだけを見て個別 patch を掘るのは後回しにし、まず Rosetta での再現・比較を優先する。

## 2026-03-13 Task 20 world generation safety decision
- `HistoricStringExpanderPatch` は当面 runtime で無効化する。
- 根拠: Rosetta 起動後、`HistorySpice` 生成中に `spice reference 時間/ガラスの/遊牧民 ... wasn't a node` が大量発生し、その後 `CherubimSpawner.ReplaceDescription` の `ArgumentOutOfRangeException` と `Worships.Generate` の `NullReferenceException` でワールド生成が停止した。
- これは `HistoricStringExpanderPatch` が表示文だけでなく history generation 用の symbolic key まで翻訳して破壊している仮説と最も整合する。
- 当面は playability を coverage より優先し、world generation が通ることを先に確保する。歴史文ローカライズは後で表示専用ケースに限定して再導入する。
