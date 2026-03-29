using System.Collections.Generic;

namespace QudJP.Tests.DummyTargets;

internal sealed class DummyCyberneticsTerminalScreenTarget
{
    public bool OriginalExecuted { get; private set; }

    public string FooterText { get; set; } = string.Empty;

    public DummyUITextSkin footerTextSkin = new DummyUITextSkin();

    public List<DummyMenuOption> keyMenuOptions = new List<DummyMenuOption>();

    public void Show()
    {
        OriginalExecuted = true;
        footerTextSkin.SetText(FooterText);
        keyMenuOptions.Clear();
        keyMenuOptions.Add(new DummyMenuOption("navigate", "NavigationXYAxis"));
        keyMenuOptions.Add(new DummyMenuOption("accept", "Accept"));
        keyMenuOptions.Add(new DummyMenuOption("quit", "Cancel"));
    }
}
