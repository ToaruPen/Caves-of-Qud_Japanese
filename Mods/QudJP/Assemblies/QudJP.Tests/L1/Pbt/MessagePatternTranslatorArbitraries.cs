using FsCheck;
using FsCheck.Fluent;

namespace QudJP.Tests.L1.Pbt;

public sealed record HitWithRollPatternCase(string Source, string ExpectedTranslated);

public sealed record HitTargetWithWeaponDamagePatternCase(string Source, string ExpectedTranslated);

public sealed record HitWeaponMultiplierDamagePatternCase(string Source, string ExpectedTranslated);

public sealed record HitMultiplierWithWeaponPatternCase(string Source, string ExpectedTranslated);

public sealed record IncomingHitWeaponMultiplierDamagePatternCase(string Source, string ExpectedTranslated);

public sealed record ThirdPartyHitWeaponMultiplierDamagePatternCase(string Source, string ExpectedTranslated);

public sealed record WeaponMissPatternCase(string Source, string ExpectedTranslated);

public sealed record SpecificBleedingStopPatternCase(string Source, string ExpectedTranslated);

public sealed record BlockedByArticlePatternCase(
    string Source,
    string ExpectedTranslated,
    string ExpectedGenericFallbackTranslated);

public sealed record PassByArticlePatternCase(string Source, string ExpectedTranslated);

public static class MessagePatternTranslatorArbitraries
{
    private static Gen<string> SafeWeaponText()
    {
        return SafeTextFrom('刀', '剣', '槍', '光', '炎', '鋼', '青', '銅');
    }

    private static Gen<string> SafeObjectText()
    {
        return SafeTextFrom('熊', '豚', '鹿', '鱗', '岩', '筒', '芽', '樹');
    }

    private static Gen<string> SafeTextFrom(params char[] availableCharacters)
    {
        var characters = Gen.Elements(availableCharacters);
        return Gen.Choose(1, 6)
            .SelectMany(length => Gen.ArrayOf(characters, length))
            .Select(chars => new string(chars));
    }

    public static Arbitrary<HitWithRollPatternCase> HitWithRollPatternCases()
    {
        return (from weaponText in SafeWeaponText()
                from multiplier in Gen.Choose(1, 4)
                from damage in Gen.Choose(1, 12)
                from roll in Gen.Choose(1, 30)
                let multiplierText = $"x{multiplier}"
                let weapon = $"{{{{w|{weaponText}}}}}"
                let source = $"{{{{g|You hit {{{{&w|({multiplierText})}}}} for {damage} damage with your {weapon}! [{roll}]}}}}"
                let expected = $"{{{{g|{weapon}で{damage}ダメージを与えた。({{{{&w|{multiplierText}}}}}) [{roll}]}}}}"
                select new HitWithRollPatternCase(source, expected))
            .ToArbitrary();
    }

    public static Arbitrary<HitTargetWithWeaponDamagePatternCase> HitTargetWithWeaponDamagePatternCases()
    {
        return (from target in SafeObjectText()
                from weapon in SafeWeaponText()
                from damage in Gen.Choose(1, 12)
                let source = $"You hit the {target} with a {weapon} for {damage} damage!"
                let expected = $"{weapon}で{target}に{damage}ダメージを与えた"
                select new HitTargetWithWeaponDamagePatternCase(source, expected))
            .ToArbitrary();
    }

    public static Arbitrary<HitWeaponMultiplierDamagePatternCase> HitWeaponMultiplierDamagePatternCases()
    {
        return (from target in SafeObjectText()
                from weapon in SafeWeaponText()
                from multiplier in Gen.Choose(1, 4)
                from damage in Gen.Choose(1, 12)
                let source = $"You hit the {target} with a {weapon} (x{multiplier}) for {damage} damage!"
                let expected = $"{weapon}で{target}に{damage}ダメージを与えた！ (x{multiplier})"
                select new HitWeaponMultiplierDamagePatternCase(source, expected))
            .ToArbitrary();
    }

    public static Arbitrary<HitMultiplierWithWeaponPatternCase> HitMultiplierWithWeaponPatternCases()
    {
        return (from target in SafeObjectText()
                from weapon in SafeWeaponText()
                from multiplier in Gen.Choose(1, 4)
                let source = $"You hit the {target} (x{multiplier}) with the {weapon}!"
                let expected = $"{weapon}で{target}に命中した (x{multiplier})"
                select new HitMultiplierWithWeaponPatternCase(source, expected))
            .ToArbitrary();
    }

    public static Arbitrary<IncomingHitWeaponMultiplierDamagePatternCase> IncomingHitWeaponMultiplierDamagePatternCases()
    {
        return (from attacker in SafeObjectText()
                from weapon in SafeWeaponText()
                from multiplier in Gen.Choose(1, 4)
                from damage in Gen.Choose(1, 12)
                let source = $"The {attacker} hits you with the {weapon} (x{multiplier}) for {damage} damage!"
                let expected = $"{attacker}の{weapon}で{damage}ダメージを受けた！ (x{multiplier})"
                select new IncomingHitWeaponMultiplierDamagePatternCase(source, expected))
            .ToArbitrary();
    }

    public static Arbitrary<ThirdPartyHitWeaponMultiplierDamagePatternCase> ThirdPartyHitWeaponMultiplierDamagePatternCases()
    {
        return (from attacker in SafeObjectText()
                from target in SafeObjectText()
                from weapon in SafeWeaponText()
                from multiplier in Gen.Choose(1, 4)
                from damage in Gen.Choose(1, 12)
                let source = $"The {attacker} hits the {target} with the {weapon} (x{multiplier}) for {damage} damage!"
                let expected = $"{attacker}が{weapon}で{target}に{damage}ダメージを与えた！ (x{multiplier})"
                select new ThirdPartyHitWeaponMultiplierDamagePatternCase(source, expected))
            .ToArbitrary();
    }

    public static Arbitrary<WeaponMissPatternCase> WeaponMissPatternCases()
    {
        return (from weaponText in SafeWeaponText()
                from attacker in Gen.Choose(0, 20)
                from defender in Gen.Choose(0, 20)
                let weapon = $"{{{{w|{weaponText}}}}}"
                let source = $"{{{{r|You miss with your {weapon}! [{attacker} vs {defender}]}}}}"
                let expected = $"{{{{r|{weapon}での攻撃は外れた。[{attacker} vs {defender}]}}}}"
                select new WeaponMissPatternCase(source, expected))
            .ToArbitrary();
    }

    public static Arbitrary<SpecificBleedingStopPatternCase> SpecificBleedingStopPatternCases()
    {
        return (from owner in SafeObjectText()
                let source = $"One of {owner}の wounds stops bleeding."
                let expected = $"{owner}の傷のひとつの出血が止まった。"
                select new SpecificBleedingStopPatternCase(source, expected))
            .ToArbitrary();
    }

    public static Arbitrary<BlockedByArticlePatternCase> BlockedByArticlePatternCases()
    {
        return (from article in Gen.Elements("some", "a")
                from obstacle in SafeObjectText()
                let source = $"The way is blocked by {article} {obstacle}."
                let expected = $"{obstacle}が道を塞いでいる。"
                let expectedGenericFallback = $"{obstacle}に道を塞がれている。"
                select new BlockedByArticlePatternCase(source, expected, expectedGenericFallback))
            .ToArbitrary();
    }

    public static Arbitrary<PassByArticlePatternCase> PassByArticlePatternCases()
    {
        return (from thing in SafeObjectText()
                let source = $"You pass by a {thing}."
                let expected = $"{thing}のそばを通り過ぎた。"
                select new PassByArticlePatternCase(source, expected))
            .ToArbitrary();
    }
}
