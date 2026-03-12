
---
## Caves of Qud Mod Initialization Research (2026-03-12)

### Sources verified
- https://wiki.cavesofqud.com/wiki/Modding:Mod_Configuration (updated Sep 2025)
- https://wiki.cavesofqud.com/wiki/Modding:C_Sharp_Scripting
- https://wiki.cavesofqud.com/wiki/Modding:Harmony
- https://wiki.cavesofqud.com/wiki/Modding:Adding_Code_at_Startup
- GitHub: Erquint/CoQ_Centigrade, BlindGuyNW/qud-accessibility, joejhansen/Qommand-Palette

---

### Finding 1: Version format — `"0.1.0-dev"` は無効

manifest.json の `Version` フィールドは **シンプルな semver のみ** を受け付ける。
- 有効: `"0.1.0"`, `"1.2"`, `"2.0"`
- **無効**: `"0.1.0-dev"`, `"1.0.0-alpha"` (プレリリースサフィックス不可)
- MODERROR `'0.1.0-dev' is not a valid version string` は Version 検証失敗
- このエラーがモッドのロードをブロックしている可能性が高い

**Fix**: `"Version": "0.1.0"`

---

### Finding 2: `Assemblies` フィールドは公式ドキュメント外 — DLL はロードされていない

manifest.json の公式フィールド（wiki記載）:
`ID, LoadOrder, Title, Description, Tags, Version, Author, PreviewImage, Dependencies, Dependency, LoadBefore, LoadAfter, Directories`

**`Assemblies` は公式フィールドではない。** 調査したすべての実在モッドでも使用されていない:
- Centigrade: .cs ファイル直置き、Assemblies フィールドなし
- qud-accessibility: .cs ファイル + manifest.json を src/ に配置、Assemblies なし
- Qommand-Palette: .cs ファイル直置き、Assemblies なし
- QudUX-v2: .cs ファイル直置き、Assemblies なし

**結論**: `"Assemblies": ["Assemblies/QudJP.dll"]` はゲームに無視される。`QudJP.dll` はロードされない。

---

### Finding 3: DLL ロードの標準機能は未実装

JHawkley の 2021年 Wishlist で「プリコンパイル済みアセンブリのロード」が**提案**されていたが、
ゲームに実装された証拠はなし。現在のゲームは `.cs` ファイルを Roslyn でランタイムコンパイルする。

参照: https://gist.github.com/JHawkley/dfaeeb596fa5835dc37427666481f4f5

---

### Finding 4: Harmony は `[HarmonyPatch]` 属性から自動適用される

現行の CoQ に Harmony は組み込み済み（別途 Harmony Injector モッド不要）。

ゲームが .cs ファイルをコンパイルすると、`harmony.PatchAll(modAssembly)` が自動で呼ばれ、
すべての `[HarmonyPatch]`-属性クラスが適用される。

**`Init()` や `Reset()` の明示的な呼び出しは不要**（そして自動呼び出しもされない）。

---

### Finding 5: スタートアップコードのフック

| パターン | タイミング | 必要な属性 |
|---------|-----------|-----------|
| ModSensitiveCache | メインメニューロード時 + Mod設定変更時 | `[HasModSensitiveStaticCache]` on class + `[ModSensitiveCacheInit]` on method |
| GameBasedCache | New Game / Load Save のたび | `[HasGameBasedStaticCache]` on class → `static void Reset()` (自動) or `[GameBasedCacheInit]` on method |

`QudJPMod.Init()` は `[HasModSensitiveStaticCache]` + `[ModSensitiveCacheInit]` がなければ呼ばれない。
`QudJPMod.Reset()` は `[HasGameBasedStaticCache]` がなければ呼ばれない。

---

### Finding 6: ゲームのコンパイラは Roslyn に移行済み

`RoslynCSharp.dll` がゲームの Managed/ に存在する（BlindGuyNW/qud-accessibility の csproj から確認）。
少なくとも C# 9.0 をサポート (`<LangVersion>9</LangVersion>` が実際のモッドで使用)。
C# 10 (file-scoped namespace `namespace QudJP;`) のサポートは不明。

---

### Root Cause: なぜ QudJP のトレースが Player.log に出ないか

1. `Version: "0.1.0-dev"` → MODERROR → ロードがブロックまたは警告のみで継続
2. `Assemblies` フィールド無視 → `QudJP.dll` はロードされない
3. DLL がロードされないため、Harmony patches も適用されない
4. `Init()` / `Reset()` に適切な属性なし → たとえDLLがロードされても呼ばれない

**"Enabled mods: Caves of Qud 日本語化" が表示されるのは、manifest.json は認識され Title が使用されているから（Version 警告は継続するかもしれない）。しかし DLL はロードされない。**

---

### Recommended Fix (DLL ベースアプローチを維持する場合)

DLL のロード機構は 2 通り考えられる:

**Option A: Bootstrap .cs ファイル**
```csharp
// Bootstrap.cs (Mods/QudJP/ に配置 — ゲームがランタイムコンパイル)
using System.IO;
using System.Reflection;
using XRL;

namespace QudJP
{
    [HasModSensitiveStaticCache]
    public static class Bootstrap
    {
        [ModSensitiveCacheInit]
        public static void OnModLoaded()
        {
            var mod = ModManager.Mods.Find(m => m.ID == "QudJP");
            var dllPath = Path.Combine(mod.Path, "Assemblies", "QudJP.dll");
            var asm = Assembly.LoadFrom(dllPath);
            var harmony = new HarmonyLib.Harmony("com.qudjp.localization");
            harmony.PatchAll(asm);
        }
    }
}
```
ただし Bootstrap.cs はゲームのコンパイラが処理するので C# バージョン制限に注意。

**Option B: .cs ファイルに移行**
QudJP の src/*.cs をそのまま Mods/QudJP/ に配置し、ゲームにコンパイルさせる。
ただし file-scoped namespace (`namespace QudJP;`) が C# 10 機能であり、ゲームが C# 9 止まりなら変更が必要。

**Option C (推奨調査): `Assemblies` フィールドが最新ゲームで実際に動くか検証**
`build 210+` のゲームで実際に Version を `"0.1.0"` にしてテストし、DLL がロードされるか確認する。
