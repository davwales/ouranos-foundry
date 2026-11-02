using System.Collections.Generic;
using Godot;
using Ouranos.Foundry.Features.ProceduralGeneration.Nodes;
using Ouranos.Foundry.Features.ProceduralGeneration.Resources;
using Ouranos.Foundry.Features.ProceduralGeneration.Utils;

namespace Ouranos.Foundry.Demos.Persistence;

/// <summary>
/// Paints tiles into loaded chunks with <c>ChunkData.SetTile</c> (left mouse paints, right
/// erases) and draws every painted tile. <c>WorldGenerator</c> keeps the edits when a chunk
/// unloads and saves them, so the paint survives walking away and reloading the game.
/// </summary>
public partial class TilePainter : Node2D
{
    private const int Paint = -1;
    private const int Erase = 0;

    [Export]
    public WorldGenerator? Generator { get; private set; }

    [Export]
    public Color PaintColor { get; private set; } = new(1f, 0.4f, 0.1f);

    private readonly Dictionary<Vector2I, ChunkData> _chunks = [];

    public override void _Ready()
    {
        if (Generator is null)
        {
            GD.PushError("TilePainter: Generator is not assigned.");
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

    public override void _UnhandledInput(InputEvent @event)
    {
        var buttons = @event switch
        {
            InputEventMouseButton { Pressed: true } button => button.ButtonMask,
            InputEventMouseMotion motion => motion.ButtonMask,
            _ => (MouseButtonMask)0,
        };

        if ((buttons & MouseButtonMask.Left) != 0)
        {
            PaintAt(GetGlobalMousePosition(), Paint);
        }
        else if ((buttons & MouseButtonMask.Right) != 0)
        {
            PaintAt(GetGlobalMousePosition(), Erase);
        }
    }

    public override void _Draw()
    {
        var tileSize = TilePixelSize;
        foreach (var (coords, chunk) in _chunks)
        {
            var size = chunk.Descriptor.ChunkSizeTiles;
            foreach (var (index, value) in chunk.EditedTiles)
            {
                if (value == Paint)
                {
                    var tile = coords * size + new Vector2I(index % size, index / size);
                    DrawRect(new Rect2(tile * tileSize, Vector2.One * tileSize), PaintColor);
                }
            }
        }
    }

    private int TilePixelSize => Generator?.Config?.ChunkConfig?.TilePixelSize ?? 1;

    private void PaintAt(Vector2 position, int value)
    {
        var tile = (Vector2I)(position / TilePixelSize).Floor();
        foreach (var (coords, chunk) in _chunks)
        {
            var size = chunk.Descriptor.ChunkSizeTiles;
            if (ChunkMath.WorldToChunk(tile, size) == coords)
            {
                var local = tile - coords * size;
                chunk.SetTile(local.X, local.Y, value);
                QueueRedraw();
                return;
            }
        }
    }

    private void OnChunkGenerated(Vector2I coords, ChunkData chunk)
    {
        _chunks[coords] = chunk;
        QueueRedraw();
    }

    private void OnChunkUnloaded(Vector2I coords)
    {
        _chunks.Remove(coords);
        QueueRedraw();
    }
}
