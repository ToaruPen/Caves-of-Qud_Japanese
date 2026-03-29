namespace QudJP.Tests.DummyTargets;

internal sealed class DummyFilterBarCategoryButtonTarget
{
    public bool OriginalExecuted { get; private set; }

    public string Tooltip { get; set; } = string.Empty;

    public DummyUITextSkin tooltipText = new DummyUITextSkin();

    public DummyUITextSkin text = new DummyUITextSkin();

    public string category = string.Empty;

    public void SetCategory(string category, string? tooltip = null)
    {
        OriginalExecuted = true;
        this.category = category;
        Tooltip = tooltip ?? category;
        tooltipText.SetText(Tooltip);
        text.SetText(category == "*All" ? "ALL" : category);
    }
}
