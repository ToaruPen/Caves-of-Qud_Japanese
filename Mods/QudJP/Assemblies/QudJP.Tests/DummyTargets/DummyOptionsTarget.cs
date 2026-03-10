namespace QudJP.Tests.DummyTargets;

internal sealed class DummyOptionsTarget
{
    public readonly List<DummyOptionsRow> menuItems = new List<DummyOptionsRow>();

    public readonly List<DummyOptionsRow> filteredMenuItems = new List<DummyOptionsRow>();

    public static List<DummyMenuOption> defaultMenuOptions = new List<DummyMenuOption>
    {
        new DummyMenuOption("Collapse All"),
        new DummyMenuOption("Help"),
    };

    public static void ResetDefaults()
    {
        defaultMenuOptions = new List<DummyMenuOption>
        {
            new DummyMenuOption("Collapse All"),
            new DummyMenuOption("Help"),
        };
    }

    public void Show()
    {
    }
}

internal sealed class DummyOptionsRow
{
    public DummyOptionsRow(string title, string helpText)
    {
        Title = title;
        HelpText = helpText;
    }

    public string Title;

    public string HelpText;
}

internal sealed class DummyMenuOption
{
    public DummyMenuOption(string description)
    {
        Description = description;
    }

    public string Description;
}
