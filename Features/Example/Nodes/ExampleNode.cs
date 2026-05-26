using Godot;

namespace Ouranos.Foundry.Features.Example.Nodes;

[GlobalClass]
[Icon("res://Assets/icons/foundry_node.svg")]
public partial class ExampleNode : Node
{
    public override void _Ready()
    {
        base._Ready();
        GD.Print("Hello World!");
    }
}
