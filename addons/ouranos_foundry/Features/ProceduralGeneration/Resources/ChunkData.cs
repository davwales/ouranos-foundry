#nullable enable

using System;
using System.Collections.Generic;
using Godot;
using Ouranos.Foundry.Features.ProceduralGeneration.Types;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Resources;

/// <summary>
/// A generated chunk, as emitted by <c>WorldGenerator.ChunkGenerated</c>. Wraps the
/// <c>ChunkBuffer</c> that the generation passes filled.
/// </summary>
[GlobalClass]
public partial class ChunkData : Resource
{
    private readonly ChunkBuffer _buffer;
    private readonly Dictionary<int, int> _editedTiles = [];

    public ChunkData()
        : this(new(new(default, ChunkType.Wilderness, new HashSet<Guid>(), new HashSet<Guid>())))
    { }

    /// <summary>
    /// Wraps a generated buffer, then applies <paramref name="editedTiles"/> (tile index to
    /// value) as if they were set with <c>SetTile</c>. Indexes outside the chunk are skipped.
    /// </summary>
    public ChunkData(ChunkBuffer buffer, IReadOnlyDictionary<int, int>? editedTiles = null)
    {
        _buffer = buffer;
        foreach (var (index, value) in editedTiles ?? new Dictionary<int, int>())
        {
            if ((uint)index < (uint)buffer.TerrainData.Length)
            {
                buffer.TerrainData[index] = value;
                _editedTiles[index] = value;
            }
        }
    }

    public ChunkDescriptor Descriptor => _buffer.Descriptor;
    public bool HasAnyCity => _buffer.HasAnyCity;
    public bool HasAnyRoad => _buffer.HasAnyRoad;
    public IReadOnlySet<Guid> CityIds => _buffer.CityIds;
    public IReadOnlySet<Guid> RoadIds => _buffer.RoadIds;

    /// <inheritdoc cref="ChunkBuffer.PointsOfInterest"/>
    public IReadOnlyList<PointOfInterest> PointsOfInterest => _buffer.PointsOfInterest;

    /// <inheritdoc cref="ChunkBuffer.TerrainData"/>
    public int[] TerrainData => _buffer.TerrainData;

    public Vector2I Coordinates => _buffer.Coordinates;
    public ChunkType Type => _buffer.Type;

    public int GetTile(int localX, int localY)
    {
        return _buffer.GetTile(localX, localY);
    }

    /// <summary>
    /// Tiles changed with <c>SetTile</c> since the chunk was generated, as tile index
    /// (<c>y * ChunkSizeTiles + x</c>) to value. <c>WorldGenerator</c> keeps them when the
    /// chunk unloads, applies them again when it regenerates, and saves them.
    /// </summary>
    public IReadOnlyDictionary<int, int> EditedTiles => _editedTiles;

    /// <summary>Changes a tile and records the change in <c>EditedTiles</c>.</summary>
    public void SetTile(int localX, int localY, int value)
    {
        _buffer.SetTile(localX, localY, value);
        _editedTiles[localY * Descriptor.ChunkSizeTiles + localX] = value;
    }
}
