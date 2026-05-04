namespace QudJP.Tests.DummyTargets;

internal static class DummyConversationApiTarget
{
    public static string? LastText { get; private set; }

    public static string? LastGoodbye { get; private set; }

    public static string? LastQuestion { get; private set; }

    public static string? LastAnswer { get; private set; }

    public static void Reset()
    {
        LastText = null;
        LastGoodbye = null;
        LastQuestion = null;
        LastAnswer = null;
    }

    public static void AddSimpleConversationToObject(
        object obj,
        string Text,
        string Goodbye,
        string? Filter = null,
        string? FilterExtras = null,
        string? Append = null,
        bool ClearLost = false,
        bool ClearOriginal = true)
    {
        LastText = Text;
        LastGoodbye = Goodbye;
    }

    public static void AddSimpleConversationToObject(
        object obj,
        string Text,
        string Goodbye,
        string Question,
        string Answer,
        string? Filter = null,
        string? FilterExtras = null,
        string? Append = null,
        bool ClearLost = false,
        bool ClearOriginal = true)
    {
        LastText = Text;
        LastGoodbye = Goodbye;
        LastQuestion = Question;
        LastAnswer = Answer;
    }
}
