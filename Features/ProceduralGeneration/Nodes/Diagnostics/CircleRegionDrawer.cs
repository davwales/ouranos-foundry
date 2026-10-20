using Ouranos.Foundry.Features.ProceduralGeneration.Resources;
using Ouranos.Foundry.Features.ProceduralGeneration.Types;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Nodes.Diagnostics;

[GlobalClass]
public sealed partial class CircleRegionDrawer : DebugDrawer
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
    public Color BorderColor
    {
        get;
        private set
        {
            field = value;
            RequestRedraw();
        }
    } = new(1f, 1f, 1f, 0.5f);

    private readonly Dictionary<Guid, float> _cachedRadii = [];

    public override void OnWorldStateChanged(WorldState? world)
    {
        if (world is not null)
        {
            RefreshRadii(world);
        }
        else
        {
            _cachedRadii.Clear();
        }
    }

    public override void Draw(
        CanvasItem canvas,
        WorldState world,
        ProceduralGenerationConfig? config
    )
    {
        if (!Visible || config is null)
        {
            return;
        }

        foreach (var region in world.Regions)
        {
            var biomeDef = config.GetBiomeDefinition(region.BiomeId);
            var color = biomeDef.DebugColor;

            var radius = GetRegionDisplayRadius(region);
            canvas.DrawCircle(
                (Vector2)region.Center,
                radius,
                new Color(color.R, color.G, color.B, 0.3f)
            );
            canvas.DrawArc((Vector2)region.Center, radius, 0, Mathf.Tau, 32, color, 1f);

            if (ShowLabels)
            {
                DebugDrawer.DrawOutlinedString(
                    canvas,
                    (Vector2)region.Center + new Vector2(0, -radius - 6),
                    region.Name,
                    14,
                    BorderColor
                );

                DebugDrawer.DrawOutlinedString(
                    canvas,
                    (Vector2)region.Center + new Vector2(0, radius + 6),
                    biomeDef.DisplayName,
                    10,
                    BorderColor
                );
            }
        }
    }

    private void RefreshRadii(WorldState world)
    {
        _cachedRadii.Clear();
        foreach (var region in world.Regions)
        {
            var minDist = float.MaxValue;
            foreach (var other in world.Regions)
            {
                if (other.Id == region.Id)
                {
                    continue;
                }

                var dist = region.Center.DistanceSquaredTo(other.Center);
                if (dist < minDist)
                {
                    minDist = dist;
                }
            }
            _cachedRadii[region.Id] = minDist < float.MaxValue ? Mathf.Sqrt(minDist) * 0.45f : 100f;
        }
    }

    private float GetRegionDisplayRadius(Region region)
    {
        return _cachedRadii.GetValueOrDefault(region.Id, 100f);
    }
}
