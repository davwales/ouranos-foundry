#nullable enable

using Godot;
using Ouranos.Foundry.Features.ProceduralGeneration.Resources;
using Ouranos.Foundry.Features.ProceduralGeneration.Types;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Nodes.Diagnostics;

[GlobalClass]
public partial class CenterMarkerDrawer : DebugDrawer
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
        GenerationDefinitions? definitions
    )
    {
        if (!Visible)
        {
            return;
        }

        foreach (var region in world.Regions)
        {
            var c = (Vector2)region.Center;
            var size = 4f;
            canvas.DrawLine(c + new Vector2(-size, 0), c + new Vector2(size, 0), Colors.White, 1f);
            canvas.DrawLine(c + new Vector2(0, -size), c + new Vector2(0, size), Colors.White, 1f);
        }
    }
}
