#if HAS_TMP
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
#endif
using System;
#if !HAS_TMP
using System.Diagnostics;
#endif
using System.IO;
using System.Reflection;
using System.Threading;

namespace QudJP;

public static class FontManager
{
    private static int isInitialized;

    public static void Initialize()
    {
        if (Interlocked.Exchange(ref isInitialized, 1) == 1)
        {
            return;
        }

#if HAS_TMP
        try
        {
            var fontPath = ResolveFontPath();
            if (!File.Exists(fontPath))
            {
                throw new FileNotFoundException($"CJK font not found: {fontPath}", fontPath);
            }

            Debug.Log($"[QudJP] FontManager: Loading CJK font from {fontPath}");

            var fontAsset = TMP_FontAsset.CreateFontAsset(fontPath, 0, 96, 6, GlyphRenderMode.SDFAA, 4096, 4096);
#pragma warning disable CA1508 // Unity objects may be null despite non-nullable annotations
            if (fontAsset is null)
            {
                throw new InvalidOperationException("TMP_FontAsset.CreateFontAsset returned null");
            }
#pragma warning restore CA1508

            fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            fontAsset.isMultiAtlasTexturesEnabled = true;

            TMP_Settings.fallbackFontAssets ??= new System.Collections.Generic.List<TMP_FontAsset>();
            TMP_Settings.fallbackFontAssets.Add(fontAsset);

            if (!fontAsset.TryAddCharacters("日本語テスト"))
            {
                throw new InvalidOperationException("TryAddCharacters failed for probe string '日本語テスト'");
            }

            Debug.Log("[QudJP] FontManager: CJK font registered as TMP global fallback.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[QudJP] FontManager failed: {ex}");
            throw;
        }
#else
        Trace.TraceInformation("[QudJP] FontManager: TMP unavailable (CI build). Font injection skipped.");
#endif
    }

    private static string ResolveFontPath()
    {
        var asmPath = Assembly.GetExecutingAssembly().Location;
        string asmDir;
        if (string.IsNullOrWhiteSpace(asmPath))
        {
            asmDir = AppContext.BaseDirectory;
        }
        else
        {
            var dirName = Path.GetDirectoryName(asmPath);
            asmDir = string.IsNullOrWhiteSpace(dirName) ? AppContext.BaseDirectory : dirName;
        }

        var modRoot = Directory.GetParent(asmDir);
        return Path.Combine(modRoot is null ? asmDir : modRoot.FullName, "Fonts", "NotoSansCJKjp-Regular-Subset.otf");
    }
}
