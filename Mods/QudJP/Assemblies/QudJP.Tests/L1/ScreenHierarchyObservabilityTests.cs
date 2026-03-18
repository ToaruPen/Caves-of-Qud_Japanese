namespace QudJP.Tests.L1;

[TestFixture]
[Category("L1")]
public sealed class ScreenHierarchyObservabilityTests
{
    [Test]
    public void TryBuildNeighborhoodSnapshot_ReturnsFalse_ForNonComponent()
    {
        var result = ScreenHierarchyObservability.TryBuildNeighborhoodSnapshot(new object(), "probe", out var logLine);

        Assert.That(result, Is.False);
        Assert.That(logLine, Is.Null);
    }
}
