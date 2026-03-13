# Caves of Qud 日本語未反映/透明表示 調査・解決プラン

## TL;DR

> **Quick Summary**: 現状の症状は 1 つの不具合ではなく、少なくとも `未翻訳データ`, `未フックの表示経路`, `CJK フォント適用漏れ` の 3 層で発生している可能性が高い。まず実ゲーム証跡で「英語のまま」なのか「日本語だが見えていない」のかを切り分け、その後に Hook 拡張とフォント適用強化を進める。
>
> **Most likely explanation for the current symptom split**:
> - `Options` や一部 `Look` 説明が日本語になるのは、現行パッチまたは XML merge がその経路では効いているため。
> - `UI / item names / mutation descriptions / tooltips` が英語のままなのは、翻訳データ不足または未フック経路のため。
> - 「翻訳されているのに透明」は、現在の `FontManager` が `fallbackFontAssets` 追加だけで止まっており、既存 TMP/UGUI コンポーネントや別経路へ強制適用していないことが主因候補。

---

## Context

### Original Request
多くの英語が未翻訳、もしくは翻訳されていたとしても文字が見えない透明状態になっている。対象は UI、アイテム名、変異の説明文、tooltips など。ただし look で見た対象説明や options の中身などは日本語になっている。この原因解明と解決策を調べ、`.sisyphus` に新たなプランを作成する。

### Important Note
- 実際の計画ディレクトリは `.sysiphus` ではなく `.sisyphus`。
- 既存の長期ロードマップは `.sisyphus/plans/coq-jp-roadmap.md` にある。本プランはその補助として、現象の原因切り分けと是正順序に限定する。
- テスト戦略の更新版は `.sisyphus/plans/coq-jp-game-dll-tdd-remediation-plan.md` を正とし、本プランの切り分け作業も `game DLL-assisted TDD` を先行させる。

---

## Observed Facts

### 1. 翻訳カバレッジはカテゴリごとに偏っている
- `ObjectBlueprints` は **77.01%**、未訳 **1200 IDs**。一方 `Options`, `Skills`, `Mutations` は **100%**: `docs/translation-coverage-report.md:10`, `docs/translation-coverage-report.md:12`, `docs/translation-coverage-report.md:14`.
- よって「英語が残る」現象の一部は単純な未翻訳データで説明できる。

### 2. 現行パッチは一部 UI 経路しか押さえていない
- 現行 `src/Patches` で確認できる主経路は `UITextSkin.SetText(string)`, `GetDisplayNameEvent.GetFor`, `Popup`, `MainMenu`, `Options`, `Inventory`, `CharGen` 系: `Mods/QudJP/Assemblies/src/Patches/UITextSkinTranslationPatch.cs:15`, `Mods/QudJP/Assemblies/src/Patches/GetDisplayNamePatch.cs:16`, `Mods/QudJP/Assemblies/src/Patches/PopupTranslationPatch.cs:29`, `Mods/QudJP/Assemblies/src/Patches/OptionsLocalizationPatch.cs:11`.
- 現行 `src` には `Look.GenerateTooltipContent`, `Description.GetLongDescription`, `GetDisplayNameEvent.ProcessFor` へのパッチが存在しない（`src` grep で不在確認）。

### 3. ILSpy 解析では未カバーの経路が重要候補として既に列挙されている
- `Look.GenerateTooltipContent(GameObject)` はツールチップ系の候補: `docs/ilspy-analysis.md:44`, `docs/ilspy-analysis.md:64`, `docs/ilspy-analysis.md:167`.
- `Description.GetLongDescription(StringBuilder)` は長い説明文の候補: `docs/ilspy-analysis.md:27`, `docs/ilspy-analysis.md:68`, `docs/ilspy-analysis.md:196`.
- `GetDisplayNameEvent.ProcessFor(GameObject,bool)` も display name 組立の上流候補: `docs/ilspy-analysis.md:67`, `docs/ilspy-analysis.md:188`.

### 4. フォント適用は現状かなり限定的
- 現在の `FontManager` は `TMP_FontAsset.CreateFontAsset(...)` でフォントアセットを作成し、`TMP_Settings.fallbackFontAssets` に追加しているだけ: `Mods/QudJP/Assemblies/src/FontManager.cs:38`, `Mods/QudJP/Assemblies/src/FontManager.cs:46`, `Mods/QudJP/Assemblies/src/FontManager.cs:49`.
- `HAS_TMP` は `Unity.TextMeshPro.dll` が見える環境でのみ定義される条件付き定数: `Mods/QudJP/Assemblies/QudJP.csproj:51`.
- `TMP_Settings.defaultFontAsset` 差し替えや既存アセットへの fallback chain 保証、`TextMeshProUGUI.OnEnable` / `UnityEngine.UI.Text.OnEnable` への適用は現行 `src` に存在しない。

### 5. 過去の実装知見では、強制適用が必須と記録されている
- 既存 learnings では、安定した CJK 表示には `TMP_Settings.defaultFontAsset` 差し替え、既存アセットへの fallback 保証、`TextMeshProUGUI.OnEnable` / `UnityEngine.UI.Text.OnEnable` パッチが必要と記録: `.sisyphus/notepads/coq-jp-roadmap/learnings.md:279`, `.sisyphus/notepads/coq-jp-roadmap/learnings.md:288`, `.sisyphus/notepads/coq-jp-roadmap/learnings.md:291`, `.sisyphus/notepads/coq-jp-roadmap/learnings.md:316`.
- 同じメモでは `SelectableTextMenuItem` が多バイト文字で TMP mesh 0 を起こしやすいとされている: `.sisyphus/notepads/coq-jp-roadmap/learnings.md:317`.

### 6. 変異データ自体は翻訳済みの例が確認できる
- `Night Vision` は XML 側の `DisplayName="暗視"` と辞書側の `mutation:Night Vision` の両方が存在: `Mods/QudJP/Localization/Mutations.jp.xml:25`, `Mods/QudJP/Localization/Dictionaries/mutation-descriptions.ja.json:113`.
- つまり変異説明文が英語/透明なら、データ欠落だけでは説明しきれない。

### 7. BOM 付き XML/JSON が依然残っている
- ローカライズ規約では BOM は silent failure リスク: `Mods/QudJP/Localization/AGENTS.md:21`.
- 実際に `Items.jp.xml`, `Creatures.jp.xml`, `Options.jp.xml` など多数が BOM 付きとして記録されている: `migration-report.json:544`, `migration-report.json:598`, `migration-report.json:698`.
- ただし `Options` は実際に日本語表示される報告があるため、BOM は主因ではなく二次的リスクとして扱う。

### 8. 既存 `Player.log` にはデータ衛生上の警告が大量に残っている
- `.sisyphus/evidence/task-18-player-full.log` には `MODWARN` が多数あり、特に `HiddenMutations.jp.xml` の `BearerDescription` と `Skills.jp.xml` の `Load` / `DisplayName` が未使用属性として報告されている: `.sisyphus/evidence/task-18-player-full.log:175`, `.sisyphus/evidence/task-18-player-full.log:370`, `.sisyphus/evidence/task-18-player-full.log:373`.
- これらは「透明表示」の直接原因とは限らないが、期待した XML 属性がゲーム側で読まれていない可能性を示すため、Phase 3 の優先確認対象にする。

### 9. 外部資料でも「コードに埋め込まれた UI/生成文は XML だけでは翻訳できない」と確認されている
- 開発者 AlphaBeard は「多くのテキストはコードに強く絡みついていて、従来型のゲームより翻訳が難しい」と説明している（Steam Discussions, 2024-12）: background research `bg_1453063a`.
- 公式 wiki では標準 UI フォントが `Source Code Pro` であるとされ、CJK 非対応の可能性が高い: background research `bg_1453063a`.
- XUnity 系の既知事例でも、CJK グリフ非対応時は `□□□□` や非表示になると報告されている: background research `bg_1453063a`.

### 10. 進捗メモの一部は現行コードと一致しない
- `Mods/QudJP/Localization/Dictionaries/progress.md` には `SkillsAndPowersLocalizer`, `SafeStringTranslator`, `MessageQueueWorldLocalizer` など、現行 `Mods/QudJP/Assemblies/src` に存在しないコンポーネント名が含まれる。
- このファイルは歴史的参考資料として扱い、現行 SoT は `Mods/QudJP/Assemblies/src/` と実ゲーム証跡とする。

---

## Working Diagnosis

### P1. 未翻訳データ不足
**Explains best**: 「英語のまま残る」ObjectBlueprint 系のアイテム名や固有名詞の一部。

**Why this is likely**:
- `ObjectBlueprints` だけで未訳 1200 IDs が残っているため、UI 側が正しくても英語が残る余地が大きい。

### P2. 未フックの表示経路
**Explains best**: 「Look は訳されるのに tooltip / mutation details / item detail では効かない」ような経路差。

**Why this is likely**:
- `Look.GenerateTooltipContent` と `Description.GetLongDescription` は ILSpy 候補にあるが、現行パッチが存在しない。
- `GetDisplayNameEvent.GetFor` だけでは、組立上流や別 UI ルートを通る文字列を拾いきれない可能性が高い。

### P3. フォント適用漏れ
**Explains best**: 「翻訳されているはずなのに透明/見えない」。

**Why this is likely**:
- 現行 `FontManager` は global fallback 追加のみに留まり、既存 UI コンポーネントや `defaultFontAsset` 側を保証していない。
- 過去 learnings では OnEnable パッチと fallback chain 補強が必須と明記されている。

### P4. BOM/属性不整合などのデータ衛生問題
**Explains best**: 一部カテゴリだけ merge/load が不安定なケース。

**Why this is secondary**:
- リスクはあるが、`Options` のように BOM 付きでも機能している経路があるため、主因というより再現率を悪化させる要因とみなす。

---

## Plan

### Phase 0. 症状を「英語残留」と「透明表示」に分離する

**Goal**: 以後の修正を誤らないよう、見えていないのか、翻訳されていないのかを実証する。

**What to do**:
- 実ゲーム DLL を使った L2 で `hook gap` と `coverage gap` を先に絞る
- その上で手動 L3 で以下の画面をスクリーンショット化する:
  - Options 内容（比較用の正常ケース）
  - Look 説明（比較用の正常ケース）
  - Inventory の item name
  - item tooltip
  - mutation description
  - status/sidebar 周辺の怪しい UI
- `Player.log` から以下を抽出する:
  - `QudJP` 関連 error / exception
  - `Missing glyph`
  - `MODWARN`
  - `Unused attribute`
- 可能なら「同じ対象」を `Look` と tooltip の両方で開き、日本語データが片方でだけ見えることを証明する。

**Acceptance Criteria**:
- [ ] 英語残留ケースと透明表示ケースが別々に分類されている
- [ ] 各ケースに画面証跡とログ証跡が紐づいている

**Evidence**:
- `.sisyphus/evidence/text-visibility-player.log`
- `.sisyphus/evidence/text-visibility-*.png`

### Phase 1. 未フック経路の洗い出しと Hook 追加候補の確定

**Goal**: どの表示経路が tooltip / description / mutation details を組み立てているかを確定する。

**What to do**:
- `docs/ilspy-analysis.md` の候補を現行 `src/Patches` と突き合わせ、未実装を明文化する。
- 優先候補:
  - `XRL.UI.Look.GenerateTooltipContent(GameObject)`
  - `XRL.World.Parts.Description.GetLongDescription(StringBuilder)`
  - `XRL.World.GetDisplayNameEvent.ProcessFor(GameObject,bool)`
- それぞれに対して L2 DummyTarget 戦略を定義する。
- 変異説明に関しては、XML merge 経路と charged/tooltip 経路のどちらで使われるかを切り分ける。

**Acceptance Criteria**:
- [ ] tooltip / long description / mutation details の各経路に対応する Hook 候補が 1 つ以上確定
- [ ] 追加候補ごとに L2 テスト方針が書かれている

### Phase 2. フォント適用の強化

**Goal**: 「日本語文字列は入っているが描画されない」状態を潰す。

**What to do**:
- `FontManager` を fallback 追加だけでなく、少なくとも以下を検討・実装する:
  - ゲーム DLL が見えるローカルビルドで `HAS_TMP` が実際に有効になっていることをビルド証跡または `Player.log` で確認する
  - `TMP_Settings.defaultFontAsset` 差し替え
  - 既存 `TMP_FontAsset` への fallback chain 保証
  - `TextMeshProUGUI.OnEnable` での強制適用
  - `TMP_InputField.OnEnable` での適用
  - `UnityEngine.UI.Text.OnEnable` での legacy text 適用
- `SelectableTextMenuItem` 相当の毎フレーム更新 UI を個別に観察し、mesh 0 / 非表示が起きるか確認する。
- `Fonts/NotoSansCJKjp-Regular-Subset.otf` の実際の glyph coverage とサイズ制約を再確認する。

**Acceptance Criteria**:
- [ ] 同一文字列が screen/path をまたいでも不可視にならない
- [ ] `Player.log` に `Missing glyph` が出ない、または再現ケースで減少が確認できる

### Phase 3. データ衛生問題の除去

**Goal**: silent failure の温床を減らす。

**What to do**:
- `migration-report.json` で BOM 付きと判定された XML/JSON を BOM なしへ是正する。
- `scripts/check_encoding.py` と `scripts/validate_xml.py` を通し、再発防止の evidence を残す。
- `Player.log` に `Unused attribute` や merge 警告がある場合は、該当ファイルと属性を照合して不要属性を整理する。
- 優先対象は `HiddenMutations.jp.xml` の `BearerDescription`、`Skills.jp.xml` の `Load` / `DisplayName`、そのほか `MODWARN` 多発ファイル。

**Acceptance Criteria**:
- [ ] BOM 付き対象が 0
- [ ] XML/JSON の検証が pass

### Phase 4. 未翻訳データ補完

**Goal**: 英語残留そのものを減らす。

**What to do**:
- `docs/translation-coverage-report.md` を起点に `ObjectBlueprints` 未訳 1200 IDs から、実プレイに直撃するカテゴリを優先補完する。
- 優先順位:
  1. Inventory / common items
  2. Common creatures / factions / visible world objects
  3. Frequently surfaced mutation / item adjunct strings
- Player.log の missing key を辞書補完と XML 補完に振り分ける。

**Acceptance Criteria**:
- [ ] 実プレイで頻出の英語 item/creature 名が目立って減る
- [ ] カバレッジレポートの改善が数字で示せる

---

## Implementation Order

1. **Phase 0** で症状分類
2. **Phase 1** で経路を押さえる
3. **Phase 2** でフォント適用を強化
4. **Phase 3** で BOM/属性問題を掃除
5. **Phase 4** で未翻訳データを補完

この順序にする理由:
- 先に翻訳データを増やしても、未フック/未描画のままだと効果測定できない。
- 先にフォントだけ直しても、未フック経路の英語残留は消えない。

---

## Verification Strategy

### Automated
- `python scripts/check_encoding.py Mods/QudJP/Localization/`
- `python scripts/validate_xml.py Mods/QudJP/Localization/`
- `dotnet test Mods/QudJP/Assemblies/QudJP.Tests/QudJP.Tests.csproj`
- Hook 追加後は実ゲーム DLL ベースの target 解決テストと、必要に応じて DummyTarget テストを増やす

### Manual L3
- `python scripts/sync_mod.py`
- 新規ゲーム開始または再現用セーブで以下確認:
  - Options
  - Look
  - Inventory
  - Tooltip
  - Mutation description
  - Sidebar / status
- `Player.log` の `QudJP`, `Missing glyph`, `MODWARN`, `Unused attribute` を確認

---

## Non-Goals

- いきなり全 1200 未訳 ID を埋めること
- Phase 2 以前にプロシージャル文の完全翻訳へ進むこと
- 古い進捗メモをそのまま現行実装の事実として採用すること

---

## Deliverables

- `.sisyphus/evidence/` に分類済みのスクリーンショットと `Player.log`
- `docs/ilspy-analysis.md` と整合する Hook ギャップ一覧
- 追加 Hook 実装と L2 テスト
- 強化済み `FontManager` と必要なら OnEnable 系パッチ
- BOM/属性問題の解消 evidence
- 優先カテゴリの翻訳補完差分
