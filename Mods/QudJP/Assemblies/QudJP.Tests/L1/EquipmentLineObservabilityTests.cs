namespace QudJP.Tests.L1;

[TestFixture]
[Category("L1")]
public sealed class EquipmentLineObservabilityTests
{
    [Test]
    public void TryBuildState_SummarizesLineAndDataMembers()
    {
        var line = new DummyLine
        {
            itemNameText = new DummyTextSkin("feathered", null, "feathered"),
        };
        var data = new DummyData
        {
            compareItemName = "feathered フラーレンのアルメット",
            equippedLabel = "Equipped Item",
        };

        var success = EquipmentLineObservability.TryBuildState(line, data, fontApplications: 3, out var logLine);

        Assert.That(success, Is.True);
        Assert.That(logLine, Does.Contain("fontApplications=3"));
        Assert.That(logLine, Does.Contain("line.itemNameText=DummyTextSkin(text='feathered'"));
        Assert.That(logLine, Does.Contain("data.compareItemName='feathered フラーレンのアルメット'"));
    }

    private sealed class DummyLine
    {
        public DummyTextSkin? itemNameText;
    }

    private sealed class DummyData
    {
        public string? compareItemName;

        public string? equippedLabel;
    }

    private sealed class DummyTextSkin
    {
        public DummyTextSkin(string? text, string? formattedText, string? tmpText)
        {
            this.text = text;
            this.formattedText = formattedText;
            _tmp = new DummyTmp(tmpText);
        }

        public string? text { get; }

        public string? formattedText { get; }

        private DummyTmp _tmp { get; }
    }

    private sealed class DummyTmp
    {
        public DummyTmp(string? text)
        {
            this.text = text;
        }

        public string? text { get; }
    }
}
