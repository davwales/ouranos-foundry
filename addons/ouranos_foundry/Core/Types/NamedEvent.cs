#nullable enable

using Godot;

namespace Ouranos.Foundry.Core.Types;

/// <summary>
/// An event identified by name with an optional Variant payload. It's the event type
/// <c>EventBusBridge</c> carries, so GDScript and C# can exchange events through the
/// <see cref="EventBus"/>. C# code is better served by its own typed records.
/// </summary>
public sealed record NamedEvent(StringName Name, Variant Payload = default);
