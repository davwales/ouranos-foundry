using System.Collections.Generic;
using Godot;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Nodes;

/// <summary>
/// Tracks the player's position and emits signals to load/unload chunks
/// within a configurable view radius. Supports radius-based loading
/// instead of single-chunk-only loading.
/// </summary>
[GlobalClass]
[Icon("res://Assets/icons/foundry_node.svg")]
public partial class ChunkManager : Node
{
    [Signal]
    public delegate void RequestChunkGenerationEventHandler(Vector2I chunkCoords);

    [Signal]
    public delegate void RequestChunkUnloadEventHandler(Vector2I chunkCoords);

    [Export]
    public Node2D? Player { get; private set; }

    [Export]
    private int _chunkSize = 16;

    [Export]
    public int ViewDistanceChunks { get; private set; } = 3;

    private Vector2I _currentPlayerChunk = new(int.MinValue, int.MinValue);
    private readonly HashSet<Vector2I> _activeChunks = [];

    public override void _Process(double delta)
    {
        if (Player is null)
        {
            return;
        }

        var playerChunk = (Vector2I)(Player.GlobalPosition / _chunkSize);

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
    /// Sets the view distance in chunks. Called by WorldGenerator when
    /// ChunkConfig provides a value.
    /// </summary>
    public void SetViewDistance(int viewDistance)
    {
        ViewDistanceChunks = viewDistance;
    }
}
