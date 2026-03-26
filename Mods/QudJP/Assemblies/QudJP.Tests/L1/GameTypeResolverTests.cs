namespace QudJP.Tests.L1;

[TestFixture]
[Category("L1")]
public sealed class GameTypeResolverTests
{
    [Test]
    public void FindType_ReturnsFullNameMatch_WhenAvailable()
    {
        var resolved = GameTypeResolver.FindType(typeof(GameTypeResolverTests).FullName!, "DefinitelyNotTheSimpleName");

        Assert.That(resolved, Is.EqualTo(typeof(GameTypeResolverTests)));
    }

    [Test]
    public void FindType_FallsBackToSimpleName_WhenFullNameIsMissing()
    {
        var resolved = GameTypeResolver.FindType("QudJP.Tests.L1.Does.Not.Exist", nameof(GameTypeResolverTests));

        Assert.That(resolved, Is.EqualTo(typeof(GameTypeResolverTests)));
    }

    [Test]
    public void FindType_ReturnsNull_WhenNeitherNameResolves()
    {
        var resolved = GameTypeResolver.FindType("QudJP.Tests.L1.Does.Not.Exist", "NoSuchSimpleTypeName");

        Assert.That(resolved, Is.Null);
    }

    [Test]
    public void FindType_LogsWarning_WhenNeitherNameResolves()
    {
        const string fullTypeName = "QudJP.Tests.L1.Does.Not.Exist";
        const string simpleTypeName = "NoSuchSimpleTypeName";

        var output = TestTraceHelper.CaptureTrace(() =>
            Assert.That(GameTypeResolver.FindType(fullTypeName, simpleTypeName), Is.Null));

        Assert.Multiple(() =>
        {
            Assert.That(output, Does.Contain("GameTypeResolver failed to resolve type"));
            Assert.That(output, Does.Contain(fullTypeName));
            Assert.That(output, Does.Contain(simpleTypeName));
        });
    }
}
