# 翻訳追加手順

このドキュメントでは、QudJP に新しい翻訳を追加・編集する方法を説明します。

---

## 翻訳ファイルの種類

QudJP は 2 種類の翻訳ファイルを使います。

| 種類 | 場所 | 用途 |
|------|------|------|
| XML 翻訳ファイル | `Mods/QudJP/Localization/*.jp.xml` | クリーチャー名、会話、スキル説明など |
| JSON 辞書 | `Mods/QudJP/Localization/Dictionaries/*.ja.json` | UI テキスト、メニュー項目など |

---

## XML 翻訳ファイルの追加

### 1. ベースゲームの XML を確認する

翻訳対象のファイルはゲームの `StreamingAssets/Base/` にあります。ローカルに取り出すには:

```bash
python scripts/extract_base.py
```

これで `references/Base/` にゲームの XML がコピーされます。

### 2. 対応する `.jp.xml` ファイルを作成する

ベースゲームのファイル名に `.jp` を付けたファイルを `Mods/QudJP/Localization/` に作成します。

例: `StreamingAssets/Base/Creatures.xml` に対応する翻訳ファイルは:

```
Mods/QudJP/Localization/ObjectBlueprints/Creatures.jp.xml
```

### 3. XML Merge システムを使う

ゲームは `Load="Merge"` 属性を見て、ベース XML に翻訳を上書きします。翻訳したい要素だけを書けば十分です。

```xml
<?xml version="1.0" encoding="utf-8"?>
<objects>
  <object Name="Snapjaw" Load="Merge">
    <part Name="Render" DisplayName="スナップジョー" />
    <part Name="Description" Short="牙をむいた変異体の人型生物。" />
  </object>
</objects>
```

`Load="Merge"` を忘れると、ベースゲームの定義が丸ごと上書きされてしまいます。

### 4. 色コードを保全する

ゲームテキストには色コードが含まれます。翻訳時にそのまま残してください。

| 形式 | 例 | 意味 |
|------|-----|------|
| `{{W\|text}}` | `{{W\|重要}}` | 白色テキスト |
| `&G` | `&Gテキスト` | 緑色テキスト |
| `^r` | `^rテキスト` | 赤色テキスト |
| `&&` | `&&` | リテラルの `&`（色コードではない） |
| `^^` | `^^` | リテラルの `^`（色コードではない） |

```xml
<!-- 正しい: 色コードを保全 -->
<part Name="Description" Short="{{W|危険な}} 変異体。" />

<!-- 間違い: 色コードを削除 -->
<part Name="Description" Short="危険な変異体。" />
```

### 5. 変数プレースホルダーを保全する

ゲームテキストには実行時に置換される変数が含まれます。

```xml
<!-- =variable.name= 形式の変数はそのまま残す -->
<text>=creature.name= が攻撃してきた！</text>
```

---

## JSON 辞書の追加

UI テキストは JSON 辞書で管理します。

```json
{
  "New Game": "新しいゲーム",
  "Load Game": "ゲームをロード",
  "Options": "オプション",
  "Quit": "終了"
}
```

ファイルは `Mods/QudJP/Localization/Dictionaries/` に配置します。ファイル名は対象カテゴリを表す名前にしてください（例: `ui-mainmenu.ja.json`）。

---

## 翻訳カバレッジの確認

現在の翻訳カバレッジを確認するには:

```bash
# サマリーを表示
python scripts/diff_localization.py --summary

# 未翻訳のエントリだけを表示
python scripts/diff_localization.py --missing-only
```

---

## 検証ワークフロー

翻訳ファイルを追加・編集したら、以下の順で検証してください。

### エンコーディング検証

```bash
python scripts/check_encoding.py Mods/QudJP/Localization/
```

UTF-8 BOM なし、LF 改行、モジバケなしを確認します。

### XML 構造検証

```bash
python scripts/validate_xml.py Mods/QudJP/Localization/

# 警告もエラー扱いにする場合
python scripts/validate_xml.py Mods/QudJP/Localization/ --strict
```

以下を検証します:
- XML パースエラー
- 色コード `{{...}}` の対応関係
- 兄弟要素の ID/Name 重複
- 空の `<text>` 要素

---

## 用語集の参照

翻訳時は `docs/glossary.csv` を参照してください。固有名詞の表記ゆれを防ぐための承認済み訳語が収録されています。

| 列 | 内容 |
|----|------|
| English | 原文 |
| Japanese | 正式な日本語訳 |
| Short | 短縮形（スペースが限られる UI 向け） |
| Notes | 使用上の注意 |
| Status | `approved` / `review-needed` / `draft` |

`Status` が `approved` のエントリは確定訳です。`draft` は暫定訳なので、PR でフィードバックを歓迎します。

---

## よくある間違い

**`Load="Merge"` を忘れる**
ベースゲームの定義が消えてしまいます。XML 翻訳ファイルには必ず `Load="Merge"` を付けてください。

**色コードを削除する**
ゲーム内の色表示が壊れます。`{{W|...}}` や `&G` はそのまま残してください。

**BOM 付き UTF-8 で保存する**
ゲームの XML パーサーが誤動作することがあります。エディタの設定で「UTF-8 without BOM」を選んでください。VS Code では右下のエンコーディング表示から変更できます。

**変数プレースホルダーを翻訳する**
`=creature.name=` のような変数は翻訳しないでください。実行時に置換されます。
