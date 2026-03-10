# Dictionaries 概要と取り扱い

ILSpyで抽出したUI/メッセージ系テキストをContextIDごとにJSON化した辞書群です（`*.ja.json`）。必ず **UTF-8 (BOMなし) / LF** で保存してください。

## 主な辞書
| ファイル | 用途 |
| --- | --- |
| `ui-auto-generated.ja.json` | ILSpy抽出の自動生成UIや入力欄など。 |
| `ui-default.ja.json` | 基本UI文言（Back / OK / Loading など）。 |
| `ui-help.ja.json` | ヘルプ画面。 |
| `ui-keybinds.ja.json` | キー設定画面。 |
| `ui-options.ja.json` | オプション画面。 |
| `ui-popup.ja.json` | PopupMessage / Popup.Show 系。 |
| `ui-quit-hotfix.ja.json` | Quit確認ダイアログのホットフィックス。 |
| `ui-worldgen.ja.json` | ワールド生成UI。 |
| `ui-messagelog.ja.json` | メッセージログ・ログテンプレ。 |
| `mutation-descriptions.ja.json` | 変異の説明文。 |

その他: Inventory / Trade (`ui-inventory.ja.json`, `ui-trade.ja.json`)、Journal/Book (`ui-journal.ja.json`)、Skills/Powers/Tinkering/Sifrah/Achievements/Village history など。

## 翻訳・追加フロー
1. **抽出**: ILSpy展開から `scripts/extract_strings.py` で抽出（例: `py -3 scripts/extract_strings.py --source-root <ILSpy> --pattern '*.cs' --context-prefix Qud.UI --min-length 2 --output out-qud-ui.json`）。
2. **仕分け**: Hookが必要か、SetTextで直渡しかを確認し、ContextIDごとに割り当て。必要なら Harmony で ContextID を噛ませる（詳細は `Docs/pipelines/*.md`）。
3. **辞書生成**: `py -3 scripts/gen_dictionary.py out.json --id ui-auto-scan --output Mods/QudJP/Localization/Dictionaries/ui-auto-scan.ja.json` などで雛形を作成。
4. **検証**: `py -3 scripts/validate_dict.py <file>` でスキーマ確認、`py -3 scripts/check_encoding.py --fail-on-issues --path <file>` でモジバケ検知。

## JSONルール
- `meta.id` はファイル固有のID（例: `ui-worldgen`）。
- `rules.protectColorTags` と `rules.protectHtmlEntities` は通常 `true`。
- `entries` は `{ "key": "<原文>", "context": "<ContextID>", "text": "<訳文>" }`。Contextが不要な場合は省略可。
- 翻訳後は必ず `validate_dict.py` と `check_encoding.py` を通すこと。

## マークアップ注意
- 色タグは `{{R|...}}` などを保持。`{val}` プレースホルダは壊さない。
- Unity RichText (`<color=...>`) も崩さない。`SafeStringTranslator.SafeTranslate` で変換される前提。

## 追加辞書メモ
- ui-messagelog-combat.ja.json : 戦闘系メッセージログテンプレート。
- ui-inventory-legacy.ja.json : Classic UI 用インベントリ文言。
- ui-modpage.ja.json : Mod ページ/設定用文言。
- world-effects-cooking.ja.json : 料理効果（ProceduralCooking）の説明テキスト。
- world-effects-tonics.ja.json : トニック効果の説明テキスト。

