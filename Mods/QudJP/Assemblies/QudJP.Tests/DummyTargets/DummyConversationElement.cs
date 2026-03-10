namespace QudJP.Tests.DummyTargets;

internal sealed class DummyConversationElement
{
    public DummyConversationElement(string text)
    {
        Text = text;
    }

    public string Text { get; set; }

    public string GetDisplayText(bool withColor)
    {
        return withColor ? $"{{{{W|{Text}}}}}" : Text;
    }
}
