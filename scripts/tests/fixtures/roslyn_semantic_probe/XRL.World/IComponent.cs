namespace XRL.World;

public sealed class GameObject
{
}

public class IComponent<T>
{
    public void EmitMessage(string message)
    {
    }

    public static void EmitMessage(T source, string message)
    {
    }

    public void AddPlayerMessage(string message)
    {
    }
}
