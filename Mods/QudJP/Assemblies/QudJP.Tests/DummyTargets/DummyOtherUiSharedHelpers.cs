namespace QudJP.Tests.DummyTargets;

internal sealed class DummyUiIconWithGameObject
{
    public DummyActiveObject gameObject = new DummyActiveObject();

    public object? LastRenderable { get; private set; }

    public void FromRenderable(object? renderable)
    {
        LastRenderable = renderable;
    }
}
