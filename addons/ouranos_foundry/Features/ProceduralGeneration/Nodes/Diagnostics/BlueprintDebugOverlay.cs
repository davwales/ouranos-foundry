#nullable enable

using System.Linq;
using Godot;
using Ouranos.Foundry.Features.ProceduralGeneration.Resources;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Nodes.Diagnostics;

/// <summary>
/// Debug overlay for visualizing the procedural world blueprint.
/// Add DebugDrawer children (CityDrawer, RoadDrawer, StainedGlassRegionDrawer, etc.)
/// to compose the visualization layers.
/// </summary>
[GlobalClass]
[Icon("res://addons/ouranos_foundry/Assets/icons/foundry_node.svg")]
public partial class BlueprintDebugOverlay : Node2D
{
    [Export]
    public WorldGenerator? Generator { get; private set; }

    private WorldState? _cachedWorldState;
    private bool _cachedIsGenerated;
    private bool _dirty = true;

    public void MarkDirty()
    {
        _dirty = true;
    }

    public override void _Process(double delta)
    {
        if (Generator is null)
        {
            return;
        }

        var current = Generator.WorldState;
        if (
            current != _cachedWorldState
            || current.IsBlueprintGenerated != _cachedIsGenerated
            || _dirty
        )
        {
            _cachedWorldState = current;
            _cachedIsGenerated = current.IsBlueprintGenerated;
            _dirty = false;
            foreach (var drawer in GetChildren().OfType<DebugDrawer>())
            {
                drawer.OnWorldStateChanged(current);
            }

            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        if (Generator is null || !Generator.IsBlueprintGenerated)
        {
            return;
        }

        var world = Generator.WorldState;
        foreach (var drawer in GetChildren().OfType<DebugDrawer>())
        {
            drawer.Draw(this, world, Generator.Definitions);
        }
    }
}
