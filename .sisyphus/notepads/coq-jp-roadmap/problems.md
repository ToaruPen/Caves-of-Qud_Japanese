# Problems

## CJKフォント注入問題 (2026-03-12)

### 問題
CoQはTMP使用でCJK文字を「描画できるが」、デフォルトのゲームフォントにCJK文字が含まれない。
XMLに日本語テキストを入れても、フォント注入なしでは□□□（tofu）として表示される。

### 選択肢
**A: qud-xunity-autotranslatorに依存**
- ユーザーが qud-xunity-autotranslator をインストール
- AutoTranslatorConfig.ini で OverrideFontTextMeshPro=arialuni_sdf_u2019 を設定
- TMP_Font_AssetBundles.zip を別途ダウンロード・配置
- メリット: 実装不要。デメリット: ユーザー負担大、依存関係管理

**B: Harmonyパッチで直接フォントを注入**
- CoQ起動時にHarmonyでTMPフォントをCJK対応フォントに差し替える
- フォントアセット(.asset / assetbundle)をModに同梱
- メリット: 単体完結。デメリット: Unity Asset Bundle作成が複雑、ファイルサイズ増

### 未調査事項
- CoQが実際に使用しているTMPのバージョン（どのunity_sdf版が必要か不明）
- TMPフォントをHarmonyパッチで置換する実装例
- QudJPの想定ユーザー（技術力レベル）が手動設定できるか
