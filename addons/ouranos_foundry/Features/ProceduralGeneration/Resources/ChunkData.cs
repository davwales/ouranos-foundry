#nullable enable

using System;
using System.Collections.Generic;
using Godot;
using Ouranos.Foundry.Features.ProceduralGeneration.Types;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Resources;

[GlobalClass]
public partial class ChunkData : Resource
{
    public ChunkDescriptor Descriptor { get; set; } =
        new(default, ChunkType.Wilderness, new HashSet<Guid>(), new HashSet<Guid>());

    public bool HasAnyCity => Descriptor.OverlappingCityIds.Count > 0;
    public bool HasAnyRoad => Descriptor.OverlappingRoadIds.Count > 0;
    public IReadOnlySet<Guid> CityIds => Descriptor.OverlappingCityIds;
    public IReadOnlySet<Guid> RoadIds => Descriptor.OverlappingRoadIds;

    /// <summary>
    /// Terrain tile data for this chunk, indexed as [y * width + x].
    /// Terrain type values correspond to biome or tile identifiers.
    /// </summary>
    public int[] TerrainData { get; set; } = [];

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
