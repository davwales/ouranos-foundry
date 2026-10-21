#nullable enable

using Godot;
using Ouranos.Foundry.Features.ProceduralGeneration.Resources;
using Ouranos.Foundry.Features.ProceduralGeneration.Types;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Nodes.Diagnostics;

[GlobalClass]
public partial class CityDrawer : DebugDrawer
{
    [Export]
    public bool Visible
    {
        get;
        private set
        {
            field = value;
            RequestRedraw();
        }
    } = true;

    [Export]
    public bool ShowLabels
    {
        get;
        private set
        {
            field = value;
            RequestRedraw();
        }
    } = true;

    [Export]
    public Color FillColor
    {
        get;
        private set
        {
            field = value;
            RequestRedraw();
        }
    } = new(1f, 0.8f, 0f, 0.8f);

    [Export]
    public Color LabelColor
    {
        get;
        private set
        {
            field = value;
            RequestRedraw();
        }
    } = Colors.White;

    public override void Draw(
        CanvasItem canvas,
        WorldState world,
        GenerationDefinitions? definitions
    )
    {
        if (!Visible)
        {
            return;
        }

        foreach (var city in world.Cities)
        {
            var radius = city.Radius;
            var position = (Vector2)city.Position;
            canvas.DrawCircle(position, radius, FillColor);
            canvas.DrawArc(position, radius, 0, Mathf.Tau, 32, FillColor with { A = 1f }, 2f);

            if (ShowLabels)
            {
                DrawOutlinedString(
                    canvas,
                    position + new Vector2(0, -radius - 4),
                    city.Name,
                    12,
                    LabelColor
                );
                DrawOutlinedString(
                    canvas,
                    position + new Vector2(0, radius + 8),
                    city.SizeId,
                    10,
                    LabelColor
                );
            }
        }
    }
}
