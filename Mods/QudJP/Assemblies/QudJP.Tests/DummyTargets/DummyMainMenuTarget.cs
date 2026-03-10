namespace QudJP.Tests.DummyTargets;

internal sealed class DummyMainMenuTarget
{
    public static List<DummyMainMenuOption> LeftOptions = new List<DummyMainMenuOption>
    {
        new DummyMainMenuOption("Options", "Pick:Options"),
        new DummyMainMenuOption("Mods", "Pick:Installed Mod Configuration"),
    };

    public static List<DummyMainMenuOption> RightOptions = new List<DummyMainMenuOption>
    {
        new DummyMainMenuOption("Help", "Pick:Help"),
        new DummyMainMenuOption("Credits", "Pick:Credits"),
    };

    public static void ResetDefaults()
    {
        LeftOptions = new List<DummyMainMenuOption>
        {
            new DummyMainMenuOption("Options", "Pick:Options"),
            new DummyMainMenuOption("Mods", "Pick:Installed Mod Configuration"),
        };

        RightOptions = new List<DummyMainMenuOption>
        {
            new DummyMainMenuOption("Help", "Pick:Help"),
            new DummyMainMenuOption("Credits", "Pick:Credits"),
        };
    }

    public void Show()
    {
    }
}

internal sealed class DummyMainMenuOption
{
    public DummyMainMenuOption(string text, string command)
    {
        Text = text;
        Command = command;
    }

    public string Text;

    public string Command;
}
