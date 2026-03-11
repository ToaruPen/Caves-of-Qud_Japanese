namespace QudJP.Tests.DummyTargets;

internal sealed class DummyUITextSkin
{
    public string? Text { get; private set; }

    public void SetText(string text)
    {
        Text = text;
    }
}
