using System.Collections.Generic;

namespace QudJP.Tests.DummyTargets;

internal sealed class DummyGameSummaryScreenTarget
{
    public DummyUITextSkin causeText = new DummyUITextSkin();

    public DummyUITextSkin detailsText = new DummyUITextSkin();

    public List<DummyMenuOption> keyMenuOptions = new List<DummyMenuOption>();

    public void ShowGameSummary(string name, string cause, string details, bool real)
    {
        _ = name;
        _ = real;
        causeText.SetText(cause);
        detailsText.SetText(details);
    }

    public void UpdateMenuBars()
    {
        keyMenuOptions.Clear();
        keyMenuOptions.Add(new DummyMenuOption("Save Tombstone File") { InputCommand = "CmdHelp" });
        keyMenuOptions.Add(new DummyMenuOption("Exit") { InputCommand = "Cancel" });
    }
}

internal static class DummyAsleepMessageTarget
{
    public static string FallAsleep()
    {
        return "You fall {{C|asleep}}!";
    }

    public static string AreAsleep()
    {
        return "You are asleep.";
    }
}
