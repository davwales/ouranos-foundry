#nullable enable

using System.Collections.Generic;
using Godot;
using Ouranos.Foundry.Features.ProceduralGeneration.Utils;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Nodes;

/// <summary>
/// Tracks the player's position and emits signals to load/unload chunks
/// within a configurable view radius. Supports radius-based loading
/// instead of single-chunk-only loading.
/// </summary>
[GlobalClass]
[Icon("res://addons/ouranos_foundry/Assets/icons/foundry_node.svg")]
public partial class ChunkManager : Node
{
    [Signal]
    public delegate void RequestChunkGenerationEventHandler(Vector2I chunkCoords);

    [Signal]
    public delegate void RequestChunkUnloadEventHandler(Vector2I chunkCoords);

    [Export]
    public Node2D? Player { get; private set; }

    [Export]
    public int ViewDistanceChunks { get; private set; } = 3;

    private int _chunkSizePixels = 16;
    private static readonly Vector2I NoChunk = new(int.MinValue, int.MinValue);

    private Vector2I _currentPlayerChunk = NoChunk;
    private readonly HashSet<Vector2I> _activeChunks = [];

    public override void _Process(double delta)
    {
        if (Player is null)
        {
            return;
        }

        var playerChunk = ChunkMath.PixelToChunk(Player.GlobalPosition, _chunkSizePixels);

        if (playerChunk == _currentPlayerChunk)
        {
            return;
        }

        _currentPlayerChunk = playerChunk;

        var neededChunks = ComputeChunksInRadius(playerChunk, ViewDistanceChunks);

        foreach (var chunk in neededChunks)
        {
            _activeChunks.Add(chunk);
            EmitSignal(SignalName.RequestChunkGeneration, chunk);
        }

        var expiredChunks = new List<Vector2I>();
        foreach (var chunk in _activeChunks)
        {
            if (!neededChunks.Contains(chunk))
            {
                expiredChunks.Add(chunk);
            }
        }

        foreach (var chunk in expiredChunks)
        {
            _activeChunks.Remove(chunk);
            EmitSignal(SignalName.RequestChunkUnload, chunk);
        }
    }

    /// <summary>
    /// Computes all chunk coordinates within a square radius around the center.
    /// Uses a square rather than circle for simplicity and performance.
    /// </summary>
    private static HashSet<Vector2I> ComputeChunksInRadius(Vector2I center, int radius)
    {
        var chunks = new HashSet<Vector2I>();

        for (var dx = -radius; dx <= radius; dx++)
        {
            for (var dy = -radius; dy <= radius; dy++)
            {
                chunks.Add(new Vector2I(center.X + dx, center.Y + dy));
            }
        }

        return chunks;
    }

    /// <summary>
    /// Removes a chunk from the active set without emitting an unload signal.
    /// Used by WorldGenerator when it handles unloading internally.
    /// </summary>
    public void ForgetChunk(Vector2I chunkCoords)
    {
        _activeChunks.Remove(chunkCoords);
    }

    /// <summary>
    /// Forgets every active chunk without emitting unload signals, so the next
    /// <c>_Process</c> requests the whole area around the player again. Called by
    /// WorldGenerator after it regenerates the blueprint.
    /// </summary>
    public void ResetTracking()
    {
        _activeChunks.Clear();
        _currentPlayerChunk = NoChunk;
    }

    /// <summary>
    /// Sets the view distance in chunks. Called by WorldGenerator when
    /// ChunkConfig provides a value.
    /// </summary>
    public void SetViewDistance(int viewDistance)
    {
        ViewDistanceChunks = viewDistance;
    }

    /// <summary>
    /// Sets the chunk size used to map the player's position to a chunk. Called by
    /// WorldGenerator from ChunkConfig. Without it, a chunk is 16 world units wide.
    /// </summary>
    public void SetChunkSize(int chunkSizeTiles, int tilePixelSize)
    {
        _chunkSizePixels = chunkSizeTiles * tilePixelSize;
    }
}
