namespace UnityEngine;

public class Font : Object
{
    public static Font CreateDynamicFontFromOSFont(string fontname, int size) => new Font { name = fontname };
    public static Font CreateDynamicFontFromOSFont(string[] fontnames, int size) =>
        new Font { name = fontnames.Length == 0 ? string.Empty : fontnames[0] };
}
