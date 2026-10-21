using Godot;

namespace Ouranos.Foundry.Demos.ProceduralGeneration;

/// <summary>
/// Player stand-in for the procgen demo: moves with the arrow keys so
/// <c>ChunkManager</c> streams chunks around it.
/// </summary>
public partial class DemoPlayer : Node2D
{
    [Export]
    public float Speed { get; private set; } = 100f;

    [Export]
    public Color MarkerColor { get; private set; } = Colors.White;

    public override void _Process(double delta)
    {
        var direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        Position += direction * Speed * (float)delta;
    }

    public override void _Draw()
    {
        DrawCircle(Vector2.Zero, 2f, MarkerColor);
    }
}
