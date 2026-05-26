using System;
using System.Collections.Generic;
using Godot;
using Ouranos.Foundry.Features.ProceduralGeneration.Resources;
using Ouranos.Foundry.Features.ProceduralGeneration.Types;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Nodes.Generation;

/// <summary>
/// Generates city tiles and structures within chunks that overlap with cities.
/// Marks chunk type as City or Mixed when cities are present.
/// </summary>
[GlobalClass]
public partial class CityGenerationPass : GenerationPass
{
    [Export]
    public ProceduralGenerationConfig? ProcGenConfig { get; private set; }

    public override void RunPass(WorldBlueprint worldBlueprint, ChunkData chunk)
    {
        if (worldBlueprint is not WorldState worldState)
        {
            return;
        }

        if (!chunk.HasAnyCity)
        {
            return;
        }

        if (ProcGenConfig is null)
        {
            GD.PushError(
                $"{Name}: ProceduralGenerationConfig is not assigned. Add it in the inspector."
            );
            return;
        }
        var config = ProcGenConfig;
        var terrain = config.TerrainConfig ?? new TerrainConfig();
        var tileSize = config.ChunkConfig?.TileSize ?? chunk.Descriptor.TileSize;

        if (chunk.HasAnyRoad)
        {
            chunk.Descriptor = chunk.Descriptor with { Type = ChunkType.Mixed };
        }
        else
        {
            chunk.Descriptor = chunk.Descriptor with { Type = ChunkType.City };
        }

        foreach (var cityId in chunk.CityIds)
        {
            if (!worldState.CityById.TryGetValue(cityId, out var city))
            {
                continue;
            }

            OverlayCityOnChunk(chunk, city, terrain, tileSize);
        }
    }

    private static void OverlayCityOnChunk(
        ChunkData chunk,
        City city,
        TerrainConfig terrain,
        int tileSize
    )
    {
        if (chunk.TerrainData is null || chunk.TerrainData.Length == 0)
        {
            return;
        }

        var chunkWorldX = chunk.Descriptor.Coordinates.X * tileSize;
        var chunkWorldY = chunk.Descriptor.Coordinates.Y * tileSize;

        for (var y = 0; y < tileSize; y++)
        {
            for (var x = 0; x < tileSize; x++)
            {
                var worldX = chunkWorldX + x;
                var worldY = chunkWorldY + y;
                var tileWorld = new Vector2I(worldX, worldY);
                var dist = tileWorld.DistanceTo(city.Position);
                var idx = y * tileSize + x;

                if (idx >= chunk.TerrainData.Length)
                {
                    continue;
                }

                if (dist <= city.Radius)
                {
                    if (dist <= city.Radius * terrain.CityCenterRatio)
                    {
                        chunk.TerrainData[idx] = terrain.BuildingTerrainId;
                    }
                    else if (IsStreetTile(x, y, terrain.StreetGridSpacing))
                    {
                        chunk.TerrainData[idx] = terrain.StreetTerrainId;
                    }
                    else
                    {
                        chunk.TerrainData[idx] = terrain.CityTerrainId;
                    }
                }
            }
        }
    }

    private static bool IsStreetTile(int x, int y, int gridSpacing)
    {
        return x % gridSpacing == 0 || y % gridSpacing == 0;
    }
}
