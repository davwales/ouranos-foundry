using Godot;

namespace Ouranos.Foundry.Features.Example.Resources;

[GlobalClass]
public partial class ExampleResource : Resource
{
    [Export]
    public string FirstName { get; private set; } = "Unknown";

    [Export]
    public string LastName { get; private set; } = "Unknown";
}
