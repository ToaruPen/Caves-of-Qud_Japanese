using NUnit.Framework;

namespace QudJP.Tests.L1;

[TestFixture]
[Category("L1")]
public sealed class InventoryRenderObservabilityTests
{
    [Test]
    public void TryBuildInventoryLineState_ReportsFormattedPresentBucket()
    {
        var line = new DummyInventoryLine("{{W|奇妙な小物}}", "奇妙な小物");
        var data = new DummyInventoryLineData(category: false, displayName: "奇妙な小物");

        var success = InventoryRenderObservability.TryBuildInventoryLineState(line, data, out var logLine);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(logLine, Does.Contain("InventoryRenderProbe/v6[formatted-present]"));
            Assert.That(logLine, Does.Contain("display='奇妙な小物'"));
        });
    }

    [Test]
    public void TryBuildInventoryLineState_ReportsEmptyFormattedBucket()
    {
        var line = new DummyInventoryLine(string.Empty, "奇妙な小物");
        var data = new DummyInventoryLineData(category: false, displayName: "奇妙な小物");

        var success = InventoryRenderObservability.TryBuildInventoryLineState(line, data, out var logLine);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(logLine, Does.Contain("InventoryRenderProbe/v6[empty-formatted]"));
            Assert.That(logLine, Does.Contain("lens=5/5/0"));
        });
    }

    [Test]
    public void TryBuildInventoryLineState_SkipsCategoryRows()
    {
        var line = new DummyInventoryLine("カテゴリ", "カテゴリ");
        var data = new DummyInventoryLineData(category: true, displayName: "カテゴリ");

        var success = InventoryRenderObservability.TryBuildInventoryLineState(line, data, out var logLine);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.False);
            Assert.That(logLine, Is.Null);
        });
    }

    private sealed class DummyInventoryLine
    {
        public DummyInventoryLine(string formattedText, string text)
        {
            this.text = new DummyUITextSkin(formattedText, text);
        }

        public DummyUITextSkin text { get; }
    }

    private sealed class DummyInventoryLineData
    {
        public DummyInventoryLineData(bool category, string displayName)
        {
            this.category = category;
            this.displayName = displayName;
        }

        public bool category { get; }

        public string displayName { get; }
    }

    private sealed class DummyUITextSkin
    {
        public DummyUITextSkin(string formattedText, string text)
        {
            this.formattedText = formattedText;
            this.text = text;
        }

        public string text { get; }

        public string formattedText { get; }
    }
}
