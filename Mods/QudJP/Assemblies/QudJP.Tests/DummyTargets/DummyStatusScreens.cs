namespace QudJP.Tests.DummyTargets;

internal sealed class DummySkillsAndPowersStatusScreen
{
    public DummyUITextSkin spText = new DummyUITextSkin();

    public void UpdateViewFromData()
    {
        spText.SetText("Skill Points (SP): 0");
    }
}

internal sealed class DummyCharacterStatusScreen
{
    public DummyUITextSkin attributePointsText = new DummyUITextSkin();

    public DummyUITextSkin mutationPointsText = new DummyUITextSkin();

    public void UpdateViewFromData()
    {
        attributePointsText.SetText("Attribute Points: 0");
        mutationPointsText.SetText("Mutation Points: 0");
    }
}
