using QudJP.Patches;

namespace QudJP.Tests.L1;

[TestFixture]
[Category("L1")]
[NonParallelizable]
public sealed class WorldPartsFragmentTranslatorTests
{
    [TestCase("You cannot seem to interact with canteen in any way.", "canteenにはどうやっても干渉できないようだ。")]
    [TestCase("The canteen is not owned by you. Are you sure you want to drink from it?", "canteenはあなたの所有物ではない。本当にそこから飲みますか？")]
    [TestCase("You are now {{B|hydrated}}.", "あなたは今、{{B|hydrated}}。")]
    [TestCase("canteen has no drain.", "canteenには排出口がない。")]
    [TestCase("canteen is sealed.", "canteenは密閉されている。")]
    [TestCase("canteen is empty.", "canteenは空だ。")]
    [TestCase("You can't pour from a container into itself.", "それ自身に容器から注ぐことはできない。")]
    [TestCase("Do you want to empty canteen first?", "canteenを先に空にしますか？")]
    public void LiquidVolumeTranslator_TranslatesPopupFragments(string input, string expected)
    {
        var ok = LiquidVolumeFragmentTranslator.TryTranslatePopupMessage(
            input,
            nameof(WorldPartsFragmentTranslatorTests),
            "LiquidVolume",
            out var translated);

        Assert.Multiple(() =>
        {
            Assert.That(ok, Is.True);
            Assert.That(translated, Is.EqualTo(expected));
        });
    }

    [TestCase("You do not have 1 dram of sunslag.", "sunslagを1ドラム持っていない。")]
    public void ClonelingVehicleTranslator_TranslatesPopupFragments(string input, string expected)
    {
        var ok = ClonelingVehicleFragmentTranslator.TryTranslatePopupMessage(
            input,
            nameof(WorldPartsFragmentTranslatorTests),
            "WorldParts.Popup",
            out var translated);

        Assert.Multiple(() =>
        {
            Assert.That(ok, Is.True);
            Assert.That(translated, Is.EqualTo(expected));
        });
    }

    [TestCase("Your onboard systems are out of cloning draught.", "搭載システムのcloning draughtが切れている。")]
    public void ClonelingVehicleTranslator_TranslatesQueuedFragments(string input, string expected)
    {
        var ok = ClonelingVehicleFragmentTranslator.TryTranslateQueuedMessage(
            input,
            nameof(WorldPartsFragmentTranslatorTests),
            "WorldParts.Queue",
            out var translated);

        Assert.Multiple(() =>
        {
            Assert.That(ok, Is.True);
            Assert.That(translated, Is.EqualTo(expected));
        });
    }

    [TestCase("You extricate yourself from stasis pod.", "stasis podから抜け出した。")]
    [TestCase("You extricate snapjaw from stasis pod.", "stasis podからsnapjawを引き出した。")]
    public void EnclosingTranslator_TranslatesExtricatePopup(string input, string expected)
    {
        var ok = EnclosingFragmentTranslator.TryTranslatePopupMessage(
            input,
            nameof(WorldPartsFragmentTranslatorTests),
            "Enclosing",
            out var translated);

        Assert.Multiple(() =>
        {
            Assert.That(ok, Is.True);
            Assert.That(translated, Is.EqualTo(expected));
        });
    }
}
