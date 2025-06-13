using Godot;
using Godot.Collections;
using Ouranos.Foundry.Features.Example.Resources;

namespace Ouranos.Foundry.Features.Example.Nodes;

[GlobalClass]
public partial class ExampleGreeter : Node
{
    [Export] public Array<ExampleResource> Examples { get; private set; } = [];

    public override void _Ready()
    {
        base._Ready();

        foreach (var e in Examples)
        {
            GD.Print($"Hello, {e.FirstName} {e.LastName}!");
        }
    }
}