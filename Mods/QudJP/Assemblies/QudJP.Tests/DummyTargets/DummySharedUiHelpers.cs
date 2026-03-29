#pragma warning disable CS0649

using System;
using System.Collections;
using System.Collections.Generic;

namespace QudJP.Tests.DummyTargets;

internal sealed class DummyActiveObject
{
    public bool activeSelf = true;

    public bool Active => activeSelf;

    public void SetActive(bool active)
    {
        activeSelf = active;
    }
}

internal sealed class DummyEnabledObject
{
    public bool enabled;
}

internal sealed class DummyIconTarget
{
    public object? LastRenderable { get; private set; }

    public void FromRenderable(object? renderable)
    {
        LastRenderable = renderable;
    }
}

internal sealed class DummyCommandContext
{
    public object? data;

    public Dictionary<string, Action>? commandHandlers;

    public IDictionary? axisHandlers;
}
