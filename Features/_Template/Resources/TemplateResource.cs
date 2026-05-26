namespace Ouranos.Foundry.Features._Template.Resources;

/// <summary>
/// Replace with your resource description.
/// </summary>
[GlobalClass]
public partial class TemplateResource : Resource
{
    [Export]
    public string DisplayName { get; private set; } = "Default";

    [Export]
    public float Weight { get; private set; } = 1.0f;
}
