namespace QudJP.Tests.DummyTargets;

internal sealed class DummyAbilityBarAfterRenderTarget
{
    private string effectText = string.Empty;

    private string targetText = string.Empty;

    private string targetHealthText = string.Empty;

    public string NextEffectText { get; set; } = string.Empty;

    public string NextTargetText { get; set; } = string.Empty;

    public string NextTargetHealthText { get; set; } = string.Empty;

    public void AfterRender(object? core, object? sb)
    {
        _ = core;
        _ = sb;
        effectText = NextEffectText;
        targetText = NextTargetText;
        targetHealthText = NextTargetHealthText;
    }

    public string GetEffectText()
    {
        return effectText;
    }

    public string GetTargetText()
    {
        return targetText;
    }

    public string GetTargetHealthText()
    {
        return targetHealthText;
    }
}
