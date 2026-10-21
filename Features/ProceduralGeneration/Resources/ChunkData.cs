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

    public ChunkData()
        : this(new(new(default, ChunkType.Wilderness, new HashSet<Guid>(), new HashSet<Guid>())))
    { }

    public ChunkData(ChunkBuffer buffer)
    {
        _buffer = buffer;
    }

    public ChunkDescriptor Descriptor => _buffer.Descriptor;
    public bool HasAnyCity => _buffer.HasAnyCity;
    public bool HasAnyRoad => _buffer.HasAnyRoad;
    public IReadOnlySet<Guid> CityIds => _buffer.CityIds;
    public IReadOnlySet<Guid> RoadIds => _buffer.RoadIds;

    /// <inheritdoc cref="ChunkBuffer.TerrainData"/>
    public int[] TerrainData => _buffer.TerrainData;

    public Vector2I Coordinates => _buffer.Coordinates;
    public ChunkType Type => _buffer.Type;

    public int GetTile(int localX, int localY)
    {
        return _buffer.GetTile(localX, localY);
    }

    public void SetTile(int localX, int localY, int value)
    {
        _buffer.SetTile(localX, localY, value);
    }
}
