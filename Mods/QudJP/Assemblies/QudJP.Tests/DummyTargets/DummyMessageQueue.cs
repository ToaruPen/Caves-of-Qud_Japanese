namespace QudJP.Tests.DummyTargets;

internal static class DummyMessageQueue
{
    public static string LastMessage { get; private set; } = string.Empty;

    public static string LastColor { get; private set; } = string.Empty;

    public static bool LastUsePopup { get; private set; }

    public static void AddPlayerMessage(string message, string color, bool usePopup)
    {
        LastMessage = message;
        LastColor = color;
        LastUsePopup = usePopup;
    }

    public static void Reset()
    {
        LastMessage = string.Empty;
        LastColor = string.Empty;
        LastUsePopup = false;
    }
}
