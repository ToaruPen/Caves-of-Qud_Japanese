namespace QudJP.Tests.L1;

[TestFixture]
[Category("L1")]
public sealed class GetDisplayNameProcessPatchLogicTests
{
    [Test]
    public void TryTransformLegendaryDisplayName_RewritesLegendaryMarker()
    {
        var success = Patches.GetDisplayNameProcessPatch.TryTransformLegendaryDisplayName(
            current: "Oo-hoo-ho-HOO-OOO-ee-ho, legendary ヒヒ",
            primaryBase: "ヒヒ",
            out var transformed);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(transformed, Is.EqualTo("Oo-hoo-ho-HOO-OOO-ee-ho、伝説のヒヒ"));
        });
    }

    [Test]
    public void TryTransformLegendaryDisplayName_ReturnsFalse_WhenPrimaryBaseDoesNotMatchSuffix()
    {
        var success = Patches.GetDisplayNameProcessPatch.TryTransformLegendaryDisplayName(
            current: "Oo-hoo-ho-HOO-OOO-ee-ho, legendary ヒヒ",
            primaryBase: "ホワイト・エッシュ",
            out var transformed);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.False);
            Assert.That(transformed, Is.EqualTo("Oo-hoo-ho-HOO-OOO-ee-ho, legendary ヒヒ"));
        });
    }

    [Test]
    public void TryTransformLegendaryDisplayName_ReturnsFalse_WhenLegendaryMarkerMissing()
    {
        var success = Patches.GetDisplayNameProcessPatch.TryTransformLegendaryDisplayName(
            current: "瑪瑙 手袋屋 のフィギュリン",
            primaryBase: "手袋屋",
            out var transformed);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.False);
            Assert.That(transformed, Is.EqualTo("瑪瑙 手袋屋 のフィギュリン"));
        });
    }
}
