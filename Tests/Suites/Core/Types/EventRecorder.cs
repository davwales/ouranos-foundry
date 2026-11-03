using System.Collections.Generic;
using Godot;

namespace Ouranos.Foundry.Tests.Core.Types;

/// <summary>A node whose method is used as an event handler.</summary>
public partial class EventRecorder : Node
{
    public List<object?> Received { get; } = [];

    public void Record<T>(T evt)
    {
        Received.Add(evt);
    }
}
