using Godot;
using Ouranos.Foundry.Features.ProceduralGeneration.Resources;

namespace Ouranos.Foundry.Tests.Features.ProceduralGeneration.Resources;

/// <summary>A consumer-style <c>WorldState</c> subclass with an export of its own.</summary>
public partial class TaggedWorldState : WorldState
{
    [Export]
    public string Tag { get; set; } = "";
}
