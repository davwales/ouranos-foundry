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

    /// <summary>
    /// Whether this chunk has been fully generated and applied to the scene.
    /// </summary>
    public bool IsGenerated { get; set; }

    public Vector2I Coordinates => Descriptor.Coordinates;
    public ChunkType Type => Descriptor.Type;
    public HashSet<Guid> OverlappingCityIds => [.. Descriptor.OverlappingCityIds];
    public HashSet<Guid> OverlappingRoadIds => [.. Descriptor.OverlappingRoadIds];

    public int GetTile(int localX, int localY) =>
        TerrainData[localY * Descriptor.TileSize + localX];

    public void SetTile(int localX, int localY, int value) =>
        TerrainData[localY * Descriptor.TileSize + localX] = value;

    /// <summary>
    /// Creates a copy of this ChunkData. The Descriptor (immutable record) is shared;
    /// TerrainData is deep-copied if present. IsGenerated is always reset to false.
    /// </summary>
    public virtual ChunkData Copy()
    {
        return new ChunkData
        {
            Descriptor = Descriptor,
            TerrainData = TerrainData.Length > 0 ? (int[])TerrainData.Clone() : [],
        };
    }
}
