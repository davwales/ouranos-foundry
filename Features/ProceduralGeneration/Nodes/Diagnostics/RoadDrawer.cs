#nullable enable

using System.Linq;
using Godot;
using Ouranos.Foundry.Features.ProceduralGeneration.Resources;
using Ouranos.Foundry.Features.ProceduralGeneration.Types;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Nodes.Diagnostics;

[GlobalClass]
public partial class RoadDrawer : DebugDrawer
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
        if (!Visible || definitions is null)
        {
            return;
        }

        foreach (var road in world.Roads)
        {
            var roadDef = definitions.GetRoadType(road.RoadTypeId);
            var points = road.PathPoints.Select(p => (Vector2)p).ToArray();
            canvas.DrawPolyline(points, roadDef.DebugColor, roadDef.DebugLineWidth);
        }
    }
}
