using UnityEngine;

namespace UnityEngine.UI;

public class Graphic : Behaviour
{
    public RectTransform rectTransform { get; } = new RectTransform();
}

public class Text : Graphic
{
    public string text { get; set; } = string.Empty;
    public Font? font { get; set; }
}

public static class LayoutRebuilder
{
    public static void ForceRebuildLayoutImmediate(RectTransform layoutRoot)
    {
    }
}
