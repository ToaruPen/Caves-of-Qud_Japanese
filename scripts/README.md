# scripts/ — Python ツールガイド

QudJP の翻訳ワークフローを支援する Python スクリプト群です。

**動作要件**: Python 3.12 以上

---

## スクリプト一覧

| スクリプト | 用途 |
|-----------|------|
| `check_encoding.py` | エンコーディング検証 |
| `validate_xml.py` | XML 構造検証 |
| `diff_localization.py` | 翻訳カバレッジ比較 |
| `extract_base.py` | ゲーム XML の抽出 |
| `sync_mod.py` | Mod ファイルの配備 |

---

## check_encoding.py

ローカライゼーションファイルの UTF-8 エンコーディングを検証します。

**検出する問題**:
- UTF-8 BOM（`\xef\xbb\xbf`）
- Windows 改行コード（CRLF）
- モジバケ文字（`繧` `縺` `驕` `蜒`）
- 無効な UTF-8 バイト列

**使い方**:

```bash
# Localization ディレクトリ全体を検証
python scripts/check_encoding.py Mods/QudJP/Localization/

# scripts/ ディレクトリを検証
python scripts/check_encoding.py scripts/
```

**出力例**:

```
Scanned 78 files: 78 OK, 0 issue(s)
```

問題があった場合:

```
Scanned 78 files: 77 OK, 1 issue(s)
  [BOM] Mods/QudJP/Localization/Creatures.jp.xml — UTF-8 BOM detected
```

**終了コード**: 0 = 問題なし、1 = 問題あり

---

## validate_xml.py

翻訳 XML ファイルの構造と内容を検証します。

**検出する問題**:
- XML パースエラー（致命的エラー）
- 無効な UTF-8（致命的エラー）
- 色コード `{{...}}` の対応関係の不整合（警告）
- 兄弟要素の ID/Name 重複（警告）
- 空の `<text>` 要素（警告）

**使い方**:

```bash
# ディレクトリ全体を検証
python scripts/validate_xml.py Mods/QudJP/Localization/

# 特定のファイルを検証
python scripts/validate_xml.py Mods/QudJP/Localization/ObjectBlueprints/Creatures.jp.xml

# 警告もエラー扱いにする（CI 向け）
python scripts/validate_xml.py Mods/QudJP/Localization/ --strict
```

**出力例**:

```
Checking Mods/QudJP/Localization/Creatures.jp.xml... OK
Checking Mods/QudJP/Localization/Conversations.jp.xml... 1 warning
  WARNING: Unbalanced color code at line 42
```

**終了コード**: 0 = エラーなし（`--strict` 時は警告もなし）、1 = エラーあり

---

## diff_localization.py

ベースゲームの XML と日本語翻訳 XML を比較して、翻訳カバレッジを算出します。

**前提**: ゲームがインストールされていること（`extract_base.py` でローカルに取り出すことも可能）

**使い方**:

```bash
# カテゴリ別サマリーを表示
python scripts/diff_localization.py --summary

# 未翻訳エントリのみを表示
python scripts/diff_localization.py --missing-only

# JSON 形式で出力
python scripts/diff_localization.py --json

# ゲームパスを明示的に指定
python scripts/diff_localization.py --game-base /path/to/StreamingAssets/Base --summary
```

**出力例（`--summary`）**:

```
Category              Total  Translated  Coverage
ObjectBlueprints       5220        4020    77.01%
Conversations           200         195    97.50%
Skills                  150         150   100.00%
```

**終了コード**: 0 = 正常終了、1 = エラー

---

## extract_base.py

ゲームの `StreamingAssets/Base/` XML をリポジトリの `references/Base/` にコピーします。翻訳作業の参照用です。

**前提**: Caves of Qud が macOS の Steam デフォルトパスにインストールされていること

**使い方**:

```bash
# デフォルトパスからコピー
python scripts/extract_base.py

# ゲームパスを明示的に指定
python scripts/extract_base.py --game-base /path/to/StreamingAssets/Base
```

**出力先**: `references/Base/`（`.gitignore` で除外済み）

**終了コード**: 0 = 正常終了、1 = エラー

---

## sync_mod.py

`Mods/QudJP/` の内容をゲームの Mods ディレクトリに rsync で同期します。L3 ゲームスモークテストの前に実行します。

**前提**: Caves of Qud が macOS の Steam デフォルトパスにインストールされていること

**使い方**:

```bash
# デフォルトパスに同期
python scripts/sync_mod.py

# 同期先を明示的に指定
python scripts/sync_mod.py --dest /path/to/Mods/QudJP

# ドライラン（実際にはコピーしない）
python scripts/sync_mod.py --dry-run
```

**終了コード**: 0 = 正常終了、1 = エラー

---

## テスト

```bash
# 全テストを実行
pytest scripts/tests/

# 詳細出力
pytest scripts/tests/ -v

# 特定のスクリプトのテストだけ実行
pytest scripts/tests/test_check_encoding.py
pytest scripts/tests/test_validate_xml.py
pytest scripts/tests/test_diff_localization.py
```

現在のテスト数: **53 件**

---

## リント

```bash
ruff check scripts/
```

Ruff は `select = ["ALL"]` で全ルールを有効にしています。インライン抑制が必要な場合は `# noqa: RULE -- 理由` の形式で書いてください。

---

## 典型的なワークフロー

翻訳ファイルを追加・編集した後の検証手順:

```bash
# 1. エンコーディング確認
python scripts/check_encoding.py Mods/QudJP/Localization/

# 2. XML 構造確認
python scripts/validate_xml.py Mods/QudJP/Localization/

# 3. カバレッジ確認
python scripts/diff_localization.py --summary

# 4. テスト実行
pytest scripts/tests/
```
