namespace QudJP.Tests.L2;

[TestFixture]
[Category("L2")]
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
        var resolved = GameTypeResolver.FindType("QudJP.Tests.L2.Does.Not.Exist", nameof(GameTypeResolverTests));

        Assert.That(resolved, Is.EqualTo(typeof(GameTypeResolverTests)));
    }

    [Test]
    public void FindType_ReturnsNull_WhenNeitherNameResolves()
    {
        var resolved = GameTypeResolver.FindType("QudJP.Tests.L2.Does.Not.Exist", "NoSuchSimpleTypeName");

        Assert.That(resolved, Is.Null);
    }
}
