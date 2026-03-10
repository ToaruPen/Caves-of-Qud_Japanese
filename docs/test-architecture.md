# テストアーキテクチャ

QudJP のテストは **3 層構造** で設計されています。各層は依存範囲を厳密に制限し、高速なフィードバックと確実な品質保証を両立します。

---

## 層の概要

| 層 | 名称 | HarmonyLib | UnityEngine | 実行環境 | タグ |
|----|------|-----------|------------|---------|------|
| L1 | 純粋ロジック | 禁止 | 禁止 | CI / ローカル | `[Category("L1")]` |
| L2 | Harmony 統合 | NuGet 2.4.2 | 禁止 | CI / ローカル | `[Category("L2")]` |
| L3 | ゲームスモーク | ゲーム同梱版 | 必要 | 手動のみ | なし |

---

## L1 — 純粋ロジック

**目的**: ゲームや Harmony に依存しない純粋な C# ロジックを検証する。

**制約**:
- `using HarmonyLib` を含めてはいけない
- `using UnityEngine` を含めてはいけない
- `Assembly-CSharp.dll` の型を参照してはいけない

**対象コード**:
- `Translator` — JSON 辞書の読み込みとキャッシュ
- `ColorCodePreserver` — `{{W|...}}` / `&X` / `^Y` の保全と復元
- `Grammar patches` — `GrammarPatch.cs` の 8 つのパッチ (`GrammarAPatch`, `GrammarPluralizePatch`, `GrammarMakePossessivePatch`, `GrammarMakeAndListPatch`, `GrammarMakeOrListPatch`, `GrammarSplitOfSentenceListPatch`, `GrammarInitCapsPatch`, `GrammarCardinalNumberPatch`) が和文向けに振る舞いを調整

**実行コマンド**:

```bash
dotnet test Mods/QudJP/Assemblies/QudJP.Tests/QudJP.Tests.csproj --filter TestCategory=L1
```

**現在のテスト数**: 41 件

---

## L2 — Harmony 統合

**目的**: Harmony パッチが正しく適用されることを、ゲーム型なしで検証する。

**制約**:
- HarmonyLib NuGet 2.4.2 は使用可能
- `using UnityEngine` を含めてはいけない
- `Assembly-CSharp.dll` の型を直接インスタンス化してはいけない

**DummyTarget パターン**:

ゲームの型を直接使うと `TypeInitializationException` が発生する可能性があります。代わりに、同じメソッドシグネチャを持つ DummyTarget クラスを作成してパッチを当てます。

```csharp
// 正しい: DummyTarget を使う
internal class DummyGrammar
{
    public string Pluralize(string noun) => noun + "s";
    public string MakePossessive(string noun) => noun + "'s";
}

// 間違い: ゲームの型を直接インスタンス化する
// var grammar = new XRL.Language.Grammar();  // TypeInitializationException の原因
```

L2 テストの典型的な構造:

```csharp
[TestFixture]
[Category("L2")]
public class GrammarPatchTests
{
    private Harmony _harmony = null!;

    [SetUp]
    public void SetUp()
    {
        _harmony = new Harmony("test.grammar");
        // DummyTarget にパッチを当てる
        var original = AccessTools.Method(typeof(DummyGrammar), nameof(DummyGrammar.Pluralize));
        var prefix = AccessTools.Method(typeof(GrammarPluralizePatch), "PluralizePrefix");
        _harmony.Patch(original, prefix: new HarmonyMethod(prefix));
    }

    [TearDown]
    public void TearDown()
    {
        _harmony.UnpatchAll("test.grammar");
    }

    [Test]
    public void Pluralize_ReturnsOriginalNoun_WhenPatched()
    {
        var dummy = new DummyGrammar();
        Assert.That(dummy.Pluralize("cat"), Is.EqualTo("cat"));
    }
}
```

**実行コマンド**:

```bash
dotnet test Mods/QudJP/Assemblies/QudJP.Tests/QudJP.Tests.csproj --filter TestCategory=L2
```

**現在のテスト数**: 9 件

---

## L3 — ゲームスモーク

**目的**: 実際のゲーム起動環境でエンドツーエンドのレンダリングを確認する。

**制約**:
- 手動実行のみ（CI には含めない）
- Caves of Qud v2.0.4 の実行環境が必要

**手順**:

1. `dotnet build Mods/QudJP/Assemblies/QudJP.csproj` でビルド
2. `python scripts/sync_mod.py` でゲームディレクトリに配備
3. ゲームを起動し、Mod マネージャーで QudJP を有効化
4. メインメニュー、オプション画面、ポップアップが日本語で表示されることを確認
5. `Player.log` にエラーがないことを確認

**確認ポイント**:
- メインメニューのボタンラベルが日本語になっている
- オプション画面の項目名が日本語になっている
- ゲーム内ポップアップのタイトル・本文が日本語になっている
- `Player.log` に `[QudJP]` プレフィックスのエラーがない

---

## 層境界ルール

これらのルールは CI で機械的に確認できます。

```bash
# L1 テストに HarmonyLib 参照がないことを確認
grep -r "using HarmonyLib" Mods/QudJP/Assemblies/QudJP.Tests/L1/

# L2 テストに UnityEngine 参照がないことを確認
grep -r "using UnityEngine" Mods/QudJP/Assemblies/QudJP.Tests/L2/
```

どちらも出力がゼロであることが正しい状態です。

---

## テストプロジェクト構成

```
QudJP.Tests/
├── QudJP.Tests.csproj    # net10.0, NUnit 4.3.2, Lib.Harmony 2.4.2
├── DummyTargets/          # テスト用ダミー実装
│   ├── DummyConversationElement.cs
│   ├── DummyGrammar.cs
│   ├── DummyMainMenuTarget.cs
│   ├── DummyMessageQueue.cs
│   ├── DummyOptionsTarget.cs
│   └── DummyPopupTarget.cs
├── L1/                   # 純粋ロジックテスト（HarmonyLib 参照なし）
│   ├── TranslatorTests.cs
│   ├── ColorCodePreserverTests.cs
│   ├── GrammarPatchTests.cs
│   └── StringUtilityTests.cs
└── L2/                   # Harmony 統合テスト（UnityEngine 参照なし）
    ├── HarmonyIntegrationTests.cs
    ├── MainMenuLocalizationPatchTests.cs
    ├── OptionsLocalizationPatchTests.cs
    └── PopupTranslationPatchTests.cs
```

`net10.0` テストプロジェクトから `net48` 本体への参照は `ReferenceOutputAssembly=false` と `SkipGetTargetFrameworkProperties=true` を使って警告なしで共存させています。
