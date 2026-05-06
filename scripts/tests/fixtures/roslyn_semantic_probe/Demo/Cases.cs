using TMPro;
using XRL.Messages;
using XRL.UI;
using XRL.World;

namespace Demo;

public sealed class Cases : IComponent<GameObject>
{
    private readonly string memberText = "member text";
    private readonly string name = "snapjaw";
    private readonly string[] options = { "one", "two" };
    private readonly int count = 100;

    public void Calls(
        GameObject go,
        UITextSkin skin,
        TMP_Text tmpText,
        TextMeshProUGUI tmpDerivedText,
        UnityEngine.UI.Text unityText,
        OtherText otherText)
    {
        Popup.Show("A fixed popup leaf.");
        Other.Popup.Show("A false positive popup.");
        MissingReceiver.Show("This call must stay unresolved.");
        Popup.Maybe(null);
        MessageQueue.AddPlayerMessage("You gain " + count + " XP.");
        AddPlayerMessage("Inherited message for " + name + ".");
        Wrapper.AddPlayerMessage("Wrapped static message.");
        EmitMessage($"Hello {name}");
        EmitMessage(MakeMessage());
        EmitMessage(memberText);
        EmitMessage(options[0]);
        IComponent<GameObject>.EmitMessage(go, "{{W|Marked}} =subject.T=");
        IComponent<GameObject>.EmitMessage(go, "&Ggreen");
        IComponent<GameObject>.EmitMessage(go, "<color=#44ff88>tmp</color>");
        skin.SetText(string.Format("{{{{W|{0}}}}}", name));
        tmpText.text = "<color=#44ff88>direct tmp</color>";
        tmpDerivedText.text = "Derived tmp text";
        unityText.text = "Direct " + name;
        otherText.text = "<color=#44ff88>not a UI text assignment</color>";
    }

    private string MakeMessage()
    {
        return "made message";
    }
}

public static class Wrapper
{
    public static void AddPlayerMessage(string message)
    {
        MessageQueue.AddPlayerMessage(message);
    }
}

public sealed class OtherText
{
    public string text { get; set; } = string.Empty;
}
