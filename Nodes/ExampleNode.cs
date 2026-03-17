using Godot;

namespace Ouranos.Foundry.Nodes;

[GlobalClass]
public partial class ExampleNode : Node
{
    public override void _Ready()
    {
        base._Ready();
        GD.Print("Hello World!");
    }
}