#pragma warning disable CS0649

using System;
using System.Collections.Generic;

namespace QudJP.Tests.DummyTargets;

internal sealed class DummyHelpDataRowTarget
{
    public string Description { get; set; } = string.Empty;

    public string HelpText { get; set; } = string.Empty;

    public bool Collapsed { get; set; }
}

internal sealed class DummyFallbackHelpDataRowTarget
{
    public string FallbackText { get; set; } = "help fallback";
}

internal sealed class DummyHelpRowTarget
{
    public bool OriginalExecuted { get; private set; }

    public DummyUITextSkin categoryDescription = new DummyUITextSkin();

    public DummyUITextSkin description = new DummyUITextSkin();

    public DummyUITextSkin categoryExpander = new DummyUITextSkin();

    public List<string>? keysByLength;

    public Dictionary<string, string> formattedBindings = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Interact"] = "{{W|I}}",
        ["Highlight"] = "{{W|Alt}}",
    };

    public void setData(object data)
    {
        OriginalExecuted = true;
        if (data is DummyFallbackHelpDataRowTarget fallback)
        {
            description.text = fallback.FallbackText;
            description.Apply();
            return;
        }

        if (data is not DummyHelpDataRowTarget row)
        {
            return;
        }

        keysByLength ??= new List<string> { "Highlight", "Interact" };
        categoryDescription.text = "{{C|" + row.Description.ToUpperInvariant() + "}}";
        categoryDescription.Apply();

        var value = row.HelpText;
        if (value.Contains("~Highlight", StringComparison.Ordinal))
        {
            value = value.Replace("~Highlight", "{{W|Alt}}", StringComparison.Ordinal);
        }

        foreach (var key in keysByLength)
        {
            if (formattedBindings.TryGetValue(key, out var replacement))
            {
                value = value.Replace("~" + key, replacement, StringComparison.Ordinal);
            }
        }

        description.text = value;
        description.Apply();
        description.gameObject.SetActive(!row.Collapsed);
        categoryExpander.SetText(row.Collapsed ? "{{C|[+]}}" : "{{C|[-]}}");
    }
}
