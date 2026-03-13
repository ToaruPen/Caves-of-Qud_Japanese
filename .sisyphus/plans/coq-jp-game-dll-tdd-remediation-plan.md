# Caves of Qud 日本語化: game DLL-assisted TDD 切り替え計画

## TL;DR

> **Strategy shift**: L2 を `DummyTarget only` から `game DLL-assisted TDD + DummyTarget fallback` へ切り替える。
> まず実ゲーム DLL 上で hook 解決とシグネチャ整合を自動化し、その上でパッチ本文を検証し、表示の最終確認だけを L3 に残す。
>
> **Problem sources**:
> 1. `render gap` — 日本語文字列は入っているが描画が崩れる/透明になる
> 2. `hook gap` — tooltip / long description / display name の未フック経路
> 3. `coverage gap` — `ObjectBlueprints` 未訳 1200 IDs
> 4. `data hygiene gap` — BOM / `MODWARN` / 未使用属性

---

## Evidence Base

- `QudJP.Tests` は既に `Assembly-CSharp.dll` を条件付き参照している: `Mods/QudJP/Assemblies/QudJP.Tests/QudJP.Tests.csproj:13`, `Mods/QudJP/Assemblies/QudJP.Tests/QudJP.Tests.csproj:41`
- PoC では `Assembly-CSharp.dll` 参照成功、20 型で `TypeInitializationException` 未観測: `docs/poc-results.md:16`, `docs/poc-results.md:24`
- ただし project rule 上、テスト内で `Assembly-CSharp.dll` の型を直接 instantiate しない: `AGENTS.md:87`, `Mods/QudJP/Assemblies/AGENTS.md:159`
- 現在の症状切り分けでは `render gap`, `hook gap`, `coverage gap`, `data hygiene gap` の 4 軸が整合的: `.sisyphus/plans/coq-jp-untranslated-invisible-text-plan.md:73`

---

## Updated Test Strategy

### L1
- 純粋ロジックのみ

### L2
- **L2-A: game DLL-assisted**
  - 実 DLL 上で `TargetMethod()` / `HarmonyTargetMethods()` / シグネチャ / static メソッドを検証
  - 目的は「hook が本当に現行ゲーム 2.0.4 に当たるか」を自動化すること
- **L2-B: DummyTarget fallback**
  - 実 DLL で安全に再現しにくい patch body の変換ロジックを固定する

### L3
- 実レンダリング、フォント、透明化、メッシュ崩れ、画面上の見え方だけを確認

---

## Workstreams

### WS1. Test Harness Pivot

**Goal**: 実 DLL を使う L2 テストの導線を整える。

**Tasks**:
- `docs/test-architecture.md`, `docs/contributing.md`, `README.md`, `docs/poc-results.md` を新方針に更新
- `QudJP.Tests.csproj` に、`Assembly-CSharp.dll` が存在するときだけ `HAS_GAME_DLL` を定義する条件付きコンパイル導線を追加
- `QudJP.Tests` に game-DLL-assisted テストの共通ヘルパを追加
- `Assembly-CSharp.dll` がない環境でもコンパイル/実行が破綻しない条件分岐を整える

**Acceptance Criteria**:
- [ ] 実 DLL テストと DummyTarget テストの役割が文書化されている
- [ ] game DLL がない環境でもテストプロジェクトが壊れない

**QA Scenarios**:
```text
Scenario: Harness gating stays CI-safe
  Tool: Bash
  Steps:
    1. dotnet build Mods/QudJP/Assemblies/QudJP.Tests/QudJP.Tests.csproj
    2. dotnet test Mods/QudJP/Assemblies/QudJP.Tests/QudJP.Tests.csproj --filter TestCategory=L1
    3. dotnet test Mods/QudJP/Assemblies/QudJP.Tests/QudJP.Tests.csproj --filter TestCategory=L2
  Expected Result: Build succeeds; existing L1/L2 tests still pass regardless of whether Assembly-CSharp.dll is available.
```

### WS2. Hook Gap Isolation

**Goal**: 現在の未フック経路を実 DLL ベースで確定する。

**Primary targets**:
- `XRL.UI.Look.GenerateTooltipContent(GameObject)`
- `XRL.World.Parts.Description.GetLongDescription(StringBuilder)`
- `XRL.World.GetDisplayNameEvent.ProcessFor(GameObject,bool)`

**Tasks**:
- 実 DLL 上で target 解決テストを追加
- `HAS_GAME_DLL` が無効な環境では game-DLL-assisted テストがコンパイル対象外または skip になることを明示する
- patch body の変換ロジックテストを追加
- 未実装パッチを追加

**Acceptance Criteria**:
- [ ] 各 hook 候補が実 DLL で解決確認済み
- [ ] 各 patch に L2 テストがある

**QA Scenarios**:
```text
Scenario: Real DLL target resolution
  Tool: Bash
  Steps:
    1. dotnet test Mods/QudJP/Assemblies/QudJP.Tests/QudJP.Tests.csproj --filter TestCategory=L2G
  Expected Result: With Assembly-CSharp.dll present, target-resolution/signature tests pass and prove that Look / Description / DisplayName hooks resolve on game 2.0.4.

Scenario: CI-safe no-DLL build
  Tool: Bash
  Steps:
    1. dotnet build Mods/QudJP/Assemblies/QudJP.Tests/QudJP.Tests.csproj
  Expected Result: Build still succeeds when Assembly-CSharp.dll is absent because game-DLL-assisted tests are gated.
```

### WS3. Render Gap Isolation

**Goal**: 翻訳済み文字列が見えない原因を `FontManager` と UI 適用範囲で切る。

**Tasks**:
- `HAS_TMP` 有効時の初期化経路を自動検証可能な範囲で増やす
- `defaultFontAsset`, fallback chain, OnEnable 適用の不足箇所を実装
- L3 では tooltip / mutation description / inventory で可視性確認を行う

**Acceptance Criteria**:
- [ ] 日本語文字列が画面経路ごとに見える/見えないを分類できる
- [ ] フォント適用の不足箇所がコードとテストで明文化される

**QA Scenarios**:
```text
Scenario: Render gap remains isolated
  Tool: Bash + manual game smoke
  Steps:
    1. dotnet test Mods/QudJP/Assemblies/QudJP.Tests/QudJP.Tests.csproj --filter TestCategory=L2
    2. python scripts/sync_mod.py
    3. Launch game and inspect tooltip / mutation description / inventory text
  Expected Result: L2 proves hook logic; manual run determines whether any remaining defect is rendering-only.
```

### WS4. Coverage Gap Reduction

**Goal**: 頻出英語を減らす。

**Tasks**:
- `ObjectBlueprints` 未訳を頻出カテゴリ順に埋める
- 実 DLL テストで hook 由来の英語残りとデータ未訳を分離する

**QA Scenarios**:
```text
Scenario: Coverage gap measured after hook fixes
  Tool: Bash
  Steps:
    1. python scripts/diff_localization.py --summary
  Expected Result: Coverage report remains executable and can quantify remaining untranslated blueprint IDs.
```

### WS5. Data Hygiene Cleanup

**Goal**: BOM / `MODWARN` / 未使用属性のノイズを減らす。

**Tasks**:
- BOM 除去
- `HiddenMutations.jp.xml` の `BearerDescription` 警告調査
- `Skills.jp.xml` の `Load` / `DisplayName` 警告調査

**QA Scenarios**:
```text
Scenario: Data hygiene cleanup
  Tool: Bash
  Steps:
    1. python scripts/check_encoding.py Mods/QudJP/Localization/
    2. python scripts/validate_xml.py Mods/QudJP/Localization/
  Expected Result: Encoding and XML validation pass, and warning sources are narrowed to intentional or remaining known cases.
```

---

## First Implementation Slice

最初の実装スライスは **`hook gap` の入口を game DLL-assisted TDD 化すること**。

### Slice A
- add `HAS_GAME_DLL` gating in `QudJP.Tests.csproj`
- game-DLL-assisted な L2 テストを追加
- `GetDisplayNameEvent.GetFor` / `ProcessFor`, `Look.GenerateTooltipContent`, `Description.GetLongDescription` の target 解決を実 DLL 上で確認
- `Assembly-CSharp.dll` がない環境では当該テストが compile/CI を壊さないことを先に確認する

### Slice B
- `Look` / `Description` 向け未実装パッチを追加
- DummyTarget または patch body 直接呼び出しで文字列変換を検証

### Why this slice first
- `render gap` は L3 依存が残るが、`hook gap` は先に L2 自動化でかなり圧縮できる
- `coverage gap` を先に埋めても、hook 不足だと効いているか判別しづらい

**QA Scenarios**:
```text
Scenario: First slice is CI-safe and executable
  Tool: Bash
  Steps:
    1. dotnet build Mods/QudJP/Assemblies/QudJP.Tests/QudJP.Tests.csproj
    2. dotnet test Mods/QudJP/Assemblies/QudJP.Tests/QudJP.Tests.csproj --filter TestCategory=L2G
    3. dotnet test Mods/QudJP/Assemblies/QudJP.Tests/QudJP.Tests.csproj --filter TestCategory=L2
  Expected Result: With game DLL present, L2G and L2 pass; without game DLL, build and non-L2G tests remain healthy.
```

---

## Implementation Order

1. test strategy docs update
2. game-DLL-assisted test harness
3. hook gap patch + tests
4. render gap fixes
5. data hygiene cleanup
6. coverage gap reduction

---

## Atomic Commit Strategy

1. `docs(test): align docs with game-DLL-assisted TDD`
   - `AGENTS.md`
   - `Mods/QudJP/Assemblies/AGENTS.md`
   - `docs/test-architecture.md`
   - `docs/contributing.md`
   - `docs/poc-results.md`
   - `README.md`
   - relevant `.sisyphus/plans/*.md`

2. `test(patch): add HAS_GAME_DLL harness and L2G target resolution`
   - `QudJP.Tests.csproj`
   - new L2G tests

3. `test(patch): add signature checks for hook targets`
   - extend L2G tests with parameter/return-type validation

4. `feat(patch): add look and description hook coverage`
   - `Look.GenerateTooltipContent`
   - `Description.GetLongDescription`
   - corresponding L2/L2G tests

---

## Approval Criteria for Plan Review

- [ ] project rulesを破らずに game DLL-assisted TDD へ移行できている
- [ ] `render gap / hook gap / coverage gap / data hygiene gap` が分離されている
- [ ] first slice が小さく、L2 自動化で価値を出せる
- [ ] L3 依存は「描画の最終保証」に限定されている
