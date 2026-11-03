#nullable enable

using Godot;
using Ouranos.Foundry.Core.Types;

namespace Ouranos.Foundry.Core.Nodes;

/// <summary>
/// Connects GDScript and the editor to the <see cref="EventBus"/>. While in the tree it
/// re-emits every <see cref="NamedEvent"/> published on the bus as <c>EventReceived</c>, and
/// <see cref="Publish"/> puts one on the bus. Add one as an autoload for a global
/// GDScript-facing bus, or drop one into a scene with an <see cref="EventName"/> filter and
/// connect its signal in the editor.
/// </summary>
[GlobalClass]
[Icon("res://addons/ouranos_foundry/Assets/icons/foundry_node.svg")]
public partial class EventBusBridge : Node
{
    /// <summary>Only events with this name are re-emitted. Empty: every named event.</summary>
    [Export]
    public StringName EventName { get; private set; } = "";

    [Signal]
    public delegate void EventReceivedEventHandler(StringName name, Variant payload);

    public override void _EnterTree()
    {
        // Ends by itself when this node exits the tree.
        EventBus.Subscribe<NamedEvent>(this, OnEvent);
    }

    /// <summary>Publishes a <see cref="NamedEvent"/> on the bus, reaching C# and every bridge.</summary>
    public void Publish(StringName name, Variant payload = default)
    {
        EventBus.Publish(new NamedEvent(name, payload));
    }

    private void OnEvent(NamedEvent evt)
    {
        if (EventName.IsEmpty || evt.Name == EventName)
        {
            EmitSignal(SignalName.EventReceived, evt.Name, evt.Payload);
        }
    }
}
