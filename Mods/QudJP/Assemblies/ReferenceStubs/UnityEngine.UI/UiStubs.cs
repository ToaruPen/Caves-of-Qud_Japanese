using UnityEngine;

namespace UnityEngine.UI;

public class Text : Behaviour
{
    public string text { get; set; } = string.Empty;
    public Font? font { get; set; }
    public RectTransform rectTransform { get; set; } = new RectTransform();
}

public static class LayoutRebuilder
{
    public static void ForceRebuildLayoutImmediate(RectTransform layoutRoot)
    {
    }
}
