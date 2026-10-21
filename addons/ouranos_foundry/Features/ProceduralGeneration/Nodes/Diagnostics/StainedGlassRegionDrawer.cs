#nullable enable

using System.Collections.Generic;
using Godot;
using Ouranos.Foundry.Features.ProceduralGeneration.Resources;
using Ouranos.Foundry.Features.ProceduralGeneration.Types;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Nodes.Diagnostics;

[GlobalClass]
public sealed partial class StainedGlassRegionDrawer : DebugDrawer
{
    /// <summary>World units between samples when rendering stained glass. Lower = smoother, higher = faster.</summary>
    [Export]
    public int SampleSpacing { get; private set; } = 4;

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

    /// <summary>Whether to show region labels (reserved for future use).</summary>
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

    private List<(Rect2 Rect, Color Color)>? _stainedGlassRects;

    public override void OnWorldStateChanged(WorldState? world)
    {
        _stainedGlassRects = null;
    }

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

        if (_stainedGlassRects is null)
        {
            BuildStainedGlass(world, definitions);
        }

        foreach (var (rect, color) in _stainedGlassRects!)
        {
            canvas.DrawRect(rect, color);
        }
    }

    private void BuildStainedGlass(WorldState world, GenerationDefinitions definitions)
    {
        var rects = new List<(Rect2, Color)>();
        var spacing = Mathf.Max(1, SampleSpacing);
        var cols = world.Size / spacing;
        var rows = world.Size / spacing;

        for (var y = 0; y < rows; y++)
        {
            for (var x = 0; x < cols; x++)
            {
                var worldPos = new Vector2I(x * spacing + spacing / 2, y * spacing + spacing / 2);
                var region = world.FindRegionAt(worldPos);
                if (region is null)
                {
                    continue;
                }

                var biomeDef = definitions.GetBiome(region.BiomeId);
                var color = new Color(
                    biomeDef.DebugColor.R,
                    biomeDef.DebugColor.G,
                    biomeDef.DebugColor.B,
                    0.35f
                );
                rects.Add((new Rect2(x * spacing, y * spacing, spacing, spacing), color));
            }
        }

        _stainedGlassRects = rects;
    }
}
