#pragma warning disable CS0649

namespace QudJP.Tests.DummyTargets;

internal sealed class DummyKeybindCategoryRowTarget
{
    public string CategoryId { get; set; } = string.Empty;

    public string CategoryDescription { get; set; } = string.Empty;

    public bool Collapsed { get; set; }
}

internal sealed class DummyKeybindDataRowTarget
{
    public string CategoryId { get; set; } = string.Empty;

    public string KeyId { get; set; } = string.Empty;

    public string KeyDescription { get; set; } = string.Empty;

    public string SearchWords { get; set; } = string.Empty;

    public string? Bind1 { get; set; }

    public string? Bind2 { get; set; }

    public string? Bind3 { get; set; }

    public string? Bind4 { get; set; }
}

internal sealed class DummyFallbackKeybindRowDataTarget
{
    public string FallbackText { get; set; } = "keybind fallback";
}

internal sealed class DummyKeybindBox
{
    public string? boxText;

    public bool forceUpdate;

    public DummyActiveObject gameObject = new DummyActiveObject();
}

internal sealed class DummyKeybindRowTarget
{
    public bool OriginalExecuted { get; private set; }

    public DummyActiveObject bindingDisplay = new DummyActiveObject();

    public DummyActiveObject categoryDisplay = new DummyActiveObject();

    public DummyUITextSkin categoryDescription = new DummyUITextSkin();

    public DummyUITextSkin categoryExpander = new DummyUITextSkin();

    public DummyUITextSkin description = new DummyUITextSkin();

    public DummyKeybindBox box1 = new DummyKeybindBox();

    public DummyKeybindBox box2 = new DummyKeybindBox();

    public DummyKeybindBox box3 = new DummyKeybindBox();

    public DummyKeybindBox box4 = new DummyKeybindBox();

    public object? dataRow;

    public object? categoryRow;

    public bool NavigationContextRequested { get; private set; }

    public object GetNavigationContext()
    {
        NavigationContextRequested = true;
        return new object();
    }

    public void setData(object data)
    {
        OriginalExecuted = true;
        if (data is DummyFallbackKeybindRowDataTarget fallback)
        {
            description.text = fallback.FallbackText;
            description.Apply();
            return;
        }

        if (data is DummyKeybindDataRowTarget row)
        {
            categoryDisplay.SetActive(active: false);
            bindingDisplay.SetActive(active: true);
            categoryRow = null;
            dataRow = row;
            description.text = "{{C|" + row.KeyDescription + "}}";
            description.Apply();
            ApplyBindings(row);
        }
        else if (data is DummyKeybindCategoryRowTarget category)
        {
            categoryDisplay.SetActive(active: true);
            bindingDisplay.SetActive(active: false);
            categoryRow = category;
            dataRow = null;
            categoryDescription.text = "{{C|" + category.CategoryDescription.ToUpperInvariant() + "}}";
            categoryDescription.Apply();
            categoryExpander.SetText(category.Collapsed ? "{{C|[+]}}" : "{{C|[-]}}");
        }

        _ = GetNavigationContext();
    }

    private void ApplyBindings(DummyKeybindDataRowTarget row)
    {
        if (string.IsNullOrEmpty(row.Bind1))
        {
            box1.boxText = "{{K|None}}";
            box2.boxText = "{{K|None}}";
            box3.boxText = "{{K|None}}";
            box4.boxText = "{{K|None}}";
            box2.gameObject.SetActive(active: false);
            box3.gameObject.SetActive(active: false);
            box4.gameObject.SetActive(active: false);
        }
        else
        {
            box1.boxText = "{{w|" + row.Bind1 + "}}";
            box2.gameObject.SetActive(active: true);
            if (string.IsNullOrEmpty(row.Bind2))
            {
                box2.boxText = "{{K|None}}";
                box3.boxText = "{{K|None}}";
                box4.boxText = "{{K|None}}";
                box3.gameObject.SetActive(active: false);
                box4.gameObject.SetActive(active: false);
            }
            else
            {
                box2.boxText = "{{w|" + row.Bind2 + "}}";
                box3.gameObject.SetActive(active: true);
                if (string.IsNullOrEmpty(row.Bind3))
                {
                    box3.boxText = "{{K|None}}";
                    box4.boxText = "{{K|None}}";
                    box4.gameObject.SetActive(active: false);
                }
                else
                {
                    box3.boxText = "{{w|" + row.Bind3 + "}}";
                    box4.gameObject.SetActive(active: true);
                    box4.boxText = string.IsNullOrEmpty(row.Bind4) ? "{{K|None}}" : "{{w|" + row.Bind4 + "}}";
                }
            }
        }

        box1.forceUpdate = true;
        box2.forceUpdate = true;
        box3.forceUpdate = true;
        box4.forceUpdate = true;
    }
}
