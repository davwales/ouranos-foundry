#nullable enable

using Godot;
using Ouranos.Foundry.Features.ProceduralGeneration.Resources;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Nodes.Diagnostics;

[GlobalClass]
public partial class VoronoiEdgeDrawer : DebugDrawer
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

    public override void Draw(
        CanvasItem canvas,
        WorldState world,
        ProceduralGenerationConfig? config
    )
    {
        if (!Visible)
        {
            return;
        }

        for (var i = 0; i < world.Regions.Count; i++)
        {
            for (var j = i + 1; j < world.Regions.Count; j++)
            {
                var a = (Vector2)world.Regions[i].Center;
                var b = (Vector2)world.Regions[j].Center;
                var mid = (a + b) / 2f;
                var dir = (b - a).Normalized();
                var perp = new Vector2(-dir.Y, dir.X);

                var segmentLength = a.DistanceTo(b) * 2f;
                var start = mid - perp * segmentLength / 2f;
                var end = mid + perp * segmentLength / 2f;

                canvas.DrawLine(start, end, new Color(1f, 1f, 1f, 0.3f), 1f);
            }
        }
    }
}
