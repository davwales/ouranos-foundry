namespace Ouranos.Foundry.Features._Template.Nodes;

/// <summary>
/// Replace with your node description.
/// </summary>
[GlobalClass]
[Icon("res://Assets/icons/foundry_node.svg")]
public partial class TemplateNode : Node
{
    [Export]
    public int ExampleValue { get; private set; } = 42;

    [Signal]
    public delegate void SomethingHappenedEventHandler();

    public override void _Ready()
    {
        base._Ready();
    }
}
