namespace QudJP.Tests.L1;

[TestFixture]
[Category("L1")]
public sealed class InventoryUiFieldObservabilityTests
{
    [Test]
    public void CollectEntriesForTests_SummarizesRelevantMembers()
    {
        var screen = new DummyScreen
        {
            ItemNameText = new DummyTextSkin("熊肉ジャーキー", "<w>熊肉ジャーキー</w>", "熊肉ジャーキー"),
            CompareLabel = new DummyTextSkin("This Item", null, null),
            CompareGo = new DummyGameObject("ComparePanel", activeSelf: true, activeInHierarchy: false, childCount: 3),
            NameText = new DummyComponent(new DummyGameObject("ItemNameText", activeSelf: true, activeInHierarchy: true, childCount: 0)),
            tooltipCompareGo = new DummyGameObject("TooltipComparePanel", activeSelf: false, activeInHierarchy: false, childCount: 2),
            Other = "ignored",
        };

        var entries = InventoryUiFieldObservability.CollectEntriesForTests(screen);

        Assert.That(entries, Has.Some.Contains("ItemNameText=DummyTextSkin(text='熊肉ジャーキー'"));
        Assert.That(entries, Has.Some.Contains("CompareLabel=DummyTextSkin(text='This Item'"));
        Assert.That(entries, Has.Some.Contains("CompareGo=DummyGameObject(name='ComparePanel', activeSelf=True, activeInHierarchy=False, childCount=3, children=['Child0','Child1','Child2'])"));
        Assert.That(entries, Has.Some.Contains("NameText=DummyComponent(gameObject=DummyGameObject(name='ItemNameText', activeSelf=True, activeInHierarchy=True, childCount=0))"));
        Assert.That(entries, Has.Some.Contains("tooltipCompareGo=DummyGameObject(name='TooltipComparePanel', activeSelf=False, activeInHierarchy=False, childCount=2, children=['Child0','Child1'])"));
        Assert.That(entries, Has.None.Contains("Other="));
        Assert.That(screen.CompareGo?.transform.GetChild(1).name, Is.EqualTo("Child1"));
    }

    private sealed class DummyScreen : DummyScreenBase
    {
        public DummyTextSkin? ItemNameText;

        public DummyTextSkin? CompareLabel;

        public DummyGameObject? CompareGo;

        public DummyComponent? NameText;

        public string? Other;
    }

    private class DummyScreenBase
    {
        public DummyGameObject? tooltipCompareGo;
    }

    private sealed class DummyGameObject
    {
        public DummyGameObject(string name, bool activeSelf, bool activeInHierarchy, int childCount)
        {
            this.name = name;
            this.activeSelf = activeSelf;
            this.activeInHierarchy = activeInHierarchy;
            transform = new DummyTransform(childCount);
        }

        public string name { get; }

        public bool activeSelf { get; }

        public bool activeInHierarchy { get; }

        public DummyTransform transform { get; }
    }

    private sealed class DummyTransform
    {
        public DummyTransform(int childCount)
        {
            this.childCount = childCount;
            children = new DummyChild[childCount];
            for (var index = 0; index < childCount; index++)
            {
                children[index] = new DummyChild("Child" + index.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        public int childCount { get; }

        private DummyChild[] children { get; }

        public DummyChild GetChild(int index)
        {
            return children[index];
        }
    }

    private sealed class DummyChild
    {
        public DummyChild(string name)
        {
            this.name = name;
        }

        public string name { get; }
    }

    private sealed class DummyComponent
    {
        public DummyComponent(DummyGameObject gameObject)
        {
            this.gameObject = gameObject;
        }

        public DummyGameObject gameObject { get; }
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
