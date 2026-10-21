using System.Collections.Generic;
using Godot;
using Ouranos.Foundry.Features.ProceduralGeneration.Nodes;
using Ouranos.Foundry.Features.ProceduralGeneration.Resources;

namespace Ouranos.Foundry.Demos.ProceduralGeneration;

/// <summary>
/// Outlines every chunk the <c>WorldGenerator</c> currently has loaded, so
/// streaming is visible as the player moves.
/// </summary>
public partial class ChunkStreamingView : Node2D
{
    [Export]
    public WorldGenerator? Generator { get; private set; }

    [Export]
    public Color OutlineColor { get; private set; } = new(1f, 1f, 1f, 0.35f);

    // Loaded chunk coords mapped to their size in tiles, taken from the chunk data.
    private readonly Dictionary<Vector2I, int> _loadedChunks = [];

    public override void _Ready()
    {
        if (Generator is null)
        {
            GD.PushError("ChunkStreamingView: Generator is not assigned.");
            return;
        }

        Generator.ChunkGenerated += OnChunkGenerated;
        Generator.ChunkUnloaded += OnChunkUnloaded;
    }

    public override void _ExitTree()
    {
        if (Generator is not null)
        {
            Generator.ChunkGenerated -= OnChunkGenerated;
            Generator.ChunkUnloaded -= OnChunkUnloaded;
        }
    }

    public override void _Draw()
    {
        foreach (var (coords, chunkSize) in _loadedChunks)
        {
            var rect = new Rect2(coords * chunkSize, new Vector2(chunkSize, chunkSize));
            DrawRect(rect, OutlineColor, filled: false);
        }
    }

    private void OnChunkGenerated(Vector2I coords, ChunkData chunk)
    {
        _loadedChunks[coords] = chunk.Descriptor.ChunkSizeTiles;
        QueueRedraw();
    }

    private void OnChunkUnloaded(Vector2I coords)
    {
        if (_loadedChunks.Remove(coords))
        {
            QueueRedraw();
        }
    }
}
