using System;
namespace XRL.Annals;
[Serializable]
public class branched_local_namespace_isolation : HistoricEvent
{
    public override void Generate()
    {
        string value;
        if (Random(0, 1) == 0) value = "same";
        else value = "same";
        SetEventProperty("gospel", value);
        if (Random(0, 1) == 0) SetEventProperty("gospel", "x");
        else SetEventProperty("gospel", "y");
    }
}
