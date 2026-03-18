namespace QudJP.Tests.L1;

[TestFixture]
[Category("L1")]
public sealed class InventoryActionObservabilityTests
{
    [SetUp]
    public void SetUp()
    {
        InventoryActionObservability.ResetForTests();
    }

    [Test]
    public void TryBuildDescriptionHandleEventState_ReportsCommandAndObjects()
    {
        var description = new DummyDescriptionPart(new DummyGameObject("BaseItem"));
        var inventoryActionEvent = new DummyInventoryActionEvent(
            command: "Compare",
            actor: new DummyGameObject("ActorObject"),
            item: new DummyGameObject("ComparedItem"),
            objectTarget: new DummyGameObject("TargetItem"));

        var success = InventoryActionObservability.TryBuildDescriptionHandleEventState(description, inventoryActionEvent, out var logLine);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(logLine, Does.Contain("DescriptionInventoryActionProbe"));
            Assert.That(logLine, Does.Contain("command='Compare'"));
            Assert.That(logLine, Does.Contain("parent='QudJP.Tests.L1.InventoryActionObservabilityTests+DummyGameObject:BaseItem'"));
            Assert.That(logLine, Does.Contain("item='QudJP.Tests.L1.InventoryActionObservabilityTests+DummyGameObject:ComparedItem'"));
            Assert.That(logLine, Does.Contain("target='QudJP.Tests.L1.InventoryActionObservabilityTests+DummyGameObject:TargetItem'"));
        });
    }

    [Test]
    public void TryBuildDescriptionHandleEventState_LimitsRepeatedCommandLogs()
    {
        var description = new DummyDescriptionPart(new DummyGameObject("BaseItem"));
        var inventoryActionEvent = new DummyInventoryActionEvent(
            command: "Compare",
            actor: null,
            item: new DummyGameObject("ComparedItem"),
            objectTarget: null);

        for (var index = 0; index < 5; index++)
        {
            var success = InventoryActionObservability.TryBuildDescriptionHandleEventState(description, inventoryActionEvent, out _);
            Assert.That(success, Is.True);
        }

        var suppressed = InventoryActionObservability.TryBuildDescriptionHandleEventState(description, inventoryActionEvent, out var logLine);

        Assert.Multiple(() =>
        {
            Assert.That(suppressed, Is.False);
            Assert.That(logLine, Is.Null);
        });
    }

    private sealed class DummyDescriptionPart
    {
        public DummyDescriptionPart(DummyGameObject parentObject)
        {
            ParentObject = parentObject;
        }

        public DummyGameObject ParentObject { get; }
    }

    private sealed class DummyInventoryActionEvent
    {
        public DummyInventoryActionEvent(string command, DummyGameObject? actor, DummyGameObject? item, DummyGameObject? objectTarget)
        {
            Command = command;
            Actor = actor;
            Item = item;
            ObjectTarget = objectTarget;
        }

        public string Command { get; }

        public DummyGameObject? Actor { get; }

        public DummyGameObject? Item { get; }

        public DummyGameObject? ObjectTarget { get; }
    }

    private sealed class DummyGameObject
    {
        public DummyGameObject(string blueprint)
        {
            Blueprint = blueprint;
        }

        public string Blueprint { get; }
    }
}
