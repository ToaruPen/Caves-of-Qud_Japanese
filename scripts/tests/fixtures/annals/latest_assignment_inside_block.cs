using System;

namespace XRL.Annals;

// Regression test for the inner-loop reverse fix (CR R2 #1): given multiple
// SimpleAssignments to the same local within a single sibling stmt, the
// extractor must pick the source-LATEST assignment, not the first.
//
// The expected pattern resolves to "alt" — the source-last assignment among
// the if/else descendants. Per-branch fanout (which would yield two
// candidates "second" and "alt", one per branch) is intentionally NOT asserted
// here: that architectural extension is out of scope for #430. None of the 5
// PR2a target files exhibit this shape (setter outside if/else with branch-
// distinct SimpleAssignments inside).
[Serializable]
public class LatestAssignmentInsideBlockFixture : HistoricEvent
{
    public override void Generate()
    {
        string value;
        if (Random(0, 1) == 0)
        {
            value = "first";
            value = "second";
        }
        else
        {
            value = "alt";
        }
        SetEventProperty("gospel", value);
    }
}
