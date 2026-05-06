using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace TMPro;

public enum TextOverflowModes
{
    Overflow = 0,
}

public enum TextWrappingModes
{
    Normal = 0,
    NoWrap = 1,
}

public enum FontStyles
{
    Normal = 0,
}

public enum TextAlignmentOptions
{
    TopLeft = 257,
}

public enum AtlasPopulationMode
{
    Static = 0,
    Dynamic = 1,
}

public class TMP_TextInfo
{
    public int characterCount;
    public int pageCount;
    public int materialCount;
    public TMP_MeshInfo[] meshInfo = [];
    public TMP_CharacterInfo[] characterInfo = [];
}

public struct TMP_MeshInfo
{
    public int vertexCount;
}

public struct TMP_CharacterInfo
{
    public bool isVisible;
    public char character;
    public Vector3 bottomLeft;
    public Vector3 topRight;
}

public class TMP_FontAsset : Object
{
    public Material material { get; set; } = new Material();
    public int atlasTextureCount { get; set; }
    public AtlasPopulationMode atlasPopulationMode { get; set; }
    public bool isMultiAtlasTexturesEnabled { get; set; }
    public Font? sourceFontFile { get; set; }
    public List<TMP_FontAsset>? fallbackFontAssetTable { get; set; }

    public static TMP_FontAsset CreateFontAsset(
        string fontFilePath,
        int faceIndex,
        int samplingPointSize,
        int atlasPadding,
        GlyphRenderMode renderMode,
        int atlasWidth,
        int atlasHeight)
    {
        _ = faceIndex;
        _ = samplingPointSize;
        _ = atlasPadding;
        _ = renderMode;
        _ = atlasWidth;
        _ = atlasHeight;
        return new TMP_FontAsset { name = fontFilePath };
    }

    public bool TryAddCharacters(string characters) => true;
    public bool TryAddCharacters(string characters, out string missingCharacters)
    {
        missingCharacters = string.Empty;
        return true;
    }
}

public class TMP_Text : UnityEngine.UI.Graphic
{
    public string text { get; set; } = string.Empty;
    public TMP_TextInfo textInfo { get; set; } = new TMP_TextInfo();
    public RectTransform rectTransform { get; set; } = new RectTransform();
    public TMP_FontAsset? font { get; set; }
    public Material? fontSharedMaterial { get; set; }
    public Material? fontMaterial { get; set; }
    public Color color { get; set; }
    public bool maskable { get; set; }
    public bool raycastTarget { get; set; }
    public bool havePropertiesChanged { get; set; }
    public bool richText { get; set; }
    public bool isRightToLeftText { get; set; }
    public TextOverflowModes overflowMode { get; set; }
    public TextWrappingModes textWrappingMode { get; set; }
    public FontStyles fontStyle { get; set; }
    public TextAlignmentOptions alignment { get; set; }
    public Vector4 margin { get; set; }
    public float fontSize { get; set; }
    public float fontSizeMin { get; set; }
    public float fontSizeMax { get; set; }
    public bool enableAutoSizing { get; set; }
    public float characterSpacing { get; set; }
    public float wordSpacing { get; set; }
    public float lineSpacing { get; set; }
    public float paragraphSpacing { get; set; }
    public int maxVisibleCharacters { get; set; }
    public int maxVisibleLines { get; set; }
    public int pageToDisplay { get; set; }
    public float preferredWidth { get; set; }
    public float preferredHeight { get; set; }
    public float alpha { get; set; }
    public CanvasRenderer canvasRenderer { get; set; } = new CanvasRenderer();

    public void ForceMeshUpdate(bool ignoreActiveState = false, bool forceTextReparsing = false)
    {
    }

    public void SetAllDirty()
    {
    }

    public void UpdateMeshPadding()
    {
    }

    public void RecalculateClipping()
    {
    }

    public void RecalculateMasking()
    {
    }
}

public class TextMeshProUGUI : TMP_Text
{
}

public class TextMeshPro : TMP_Text
{
}

public class TMP_SubMeshUI : Behaviour
{
    public TMP_FontAsset? fontAsset { get; set; }
    public Material? sharedMaterial { get; set; }
    public TMP_Text? textComponent { get; set; }
    public bool maskable { get; set; }
}

public class TMP_InputField : Behaviour
{
    public TMP_Text? textComponent { get; set; }
    public UnityEngine.UI.Graphic? placeholder { get; set; }
}

public static class TMP_Settings
{
    public static TMP_FontAsset? defaultFontAsset { get; set; }
    public static List<TMP_FontAsset>? fallbackFontAssets { get; set; }
}
