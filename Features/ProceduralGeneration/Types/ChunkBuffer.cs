#nullable enable

using System;
using System.Collections.Generic;
using Godot;
using Ouranos.Foundry.Features.ProceduralGeneration.Resources;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Types;

/// <summary>
/// The plain C# chunk that generation passes write to on a worker thread.
/// <c>WorldGenerator</c> wraps it in a <c>ChunkData</c> back on the main thread.
/// </summary>
public sealed class ChunkBuffer(ChunkDescriptor descriptor)
{
    public ChunkDescriptor Descriptor { get; } = descriptor;

    /// <summary>
    /// Terrain tile data for this chunk, indexed as [y * width + x].
    /// Terrain type values correspond to biome or tile identifiers.
    /// </summary>
    public int[] TerrainData { get; } =
        new int[descriptor.ChunkSizeTiles * descriptor.ChunkSizeTiles];

    public bool HasAnyCity => Descriptor.OverlappingCityIds.Count > 0;
    public bool HasAnyRoad => Descriptor.OverlappingRoadIds.Count > 0;
    public IReadOnlySet<Guid> CityIds => Descriptor.OverlappingCityIds;
    public IReadOnlySet<Guid> RoadIds => Descriptor.OverlappingRoadIds;
    public Vector2I Coordinates => Descriptor.Coordinates;
    public ChunkType Type => Descriptor.Type;

    public int GetTile(int localX, int localY)
    {
        return TerrainData[localY * Descriptor.ChunkSizeTiles + localX];
    }

    public void SetTile(int localX, int localY, int value)
    {
        TerrainData[localY * Descriptor.ChunkSizeTiles + localX] = value;
    }
}
