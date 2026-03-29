#pragma warning disable CS0649

namespace QudJP.Tests.DummyTargets;

internal sealed class DummyBody
{
    public int PartDepth { get; set; }

    public int GetPartDepth(object? part)
    {
        _ = part;
        return PartDepth;
    }
}

internal sealed class DummyBodyPart
{
    public string Name { get; set; } = "Hand";

    public bool Primary { get; set; }

    public string CardinalDescription { get; set; } = "Right Hand";

    public DummyStatusGameObject? Equipped { get; set; }

    public DummyStatusGameObject? DefaultBehavior { get; set; }

    public DummyStatusGameObject? Cybernetics { get; set; }

    public DummyBody ParentBody { get; set; } = new DummyBody();

    public string GetCardinalDescription()
    {
        return CardinalDescription;
    }
}

internal sealed class DummyFallbackEquipmentLineDataTarget
{
    public string FallbackText { get; set; } = "equipment fallback";
}

internal sealed class DummyEquipmentLineDataTarget
{
    public bool showCybernetics { get; set; }

    public DummyBodyPart? bodyPart { get; set; }

    public object? line { get; set; }
}

internal sealed class DummyEquipmentLineTarget
{
    public bool OriginalExecuted { get; private set; }

    public DummyCommandContext context = new DummyCommandContext();

    public DummyUITextSkin text = new DummyUITextSkin();

    public DummyUITextSkin itemText = new DummyUITextSkin();

    public DummyIconTarget icon = new DummyIconTarget();

    public object? tooltipGo;

    public object? tooltipCompareGo;

    public bool isDefaultBehavior;

    public void setData(object data)
    {
        OriginalExecuted = true;
        tooltipCompareGo = null;
        context.data = data;

        if (data is DummyFallbackEquipmentLineDataTarget fallback)
        {
            itemText.SetText(fallback.FallbackText);
            return;
        }

        if (data is not DummyEquipmentLineDataTarget lineData || lineData.bodyPart is null)
        {
            return;
        }

        lineData.line = this;
        tooltipGo = lineData.showCybernetics
            ? lineData.bodyPart.Cybernetics
            : lineData.bodyPart.Equipped ?? lineData.bodyPart.DefaultBehavior;
        var prefix = lineData.bodyPart.Primary ? "{{G|*}}" : string.Empty;
        var cardinalDescription = lineData.bodyPart.GetCardinalDescription();
        text.SetText(prefix + cardinalDescription);
        var item = lineData.showCybernetics
            ? lineData.bodyPart.Cybernetics
            : lineData.bodyPart.Equipped ?? lineData.bodyPart.DefaultBehavior;
        itemText.SetText(item?.DisplayName ?? "{{K|-}}");
        icon.FromRenderable(item?.RenderForUI("Equipment"));
    }
}
