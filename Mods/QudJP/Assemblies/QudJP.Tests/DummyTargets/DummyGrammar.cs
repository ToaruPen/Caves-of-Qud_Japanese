namespace QudJP.Tests.DummyTargets;

internal static class DummyGrammar
{
    public static string A(string name, bool capitalize)
    {
        var article = name.Length > 0 && "aeiouAEIOU".IndexOf(name[0]) >= 0 ? "an" : "a";
        if (capitalize)
        {
            article = char.ToUpperInvariant(article[0]) + article[1..];
        }

        return $"{article} {name}";
    }

    public static string Pluralize(string word)
    {
        return word + "s";
    }

    public static string MakePossessive(string word)
    {
        return word.EndsWith("s", System.StringComparison.Ordinal) ? word + "'" : word + "'s";
    }
}
