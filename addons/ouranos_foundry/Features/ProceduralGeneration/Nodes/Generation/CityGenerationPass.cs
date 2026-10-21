#nullable enable

using Godot;
using Ouranos.Foundry.Core.Attributes;
using Ouranos.Foundry.Features.ProceduralGeneration.Resources;
using Ouranos.Foundry.Features.ProceduralGeneration.Types;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Nodes.Generation;

/// <summary>
/// Generates city tiles and structures within chunks that overlap with cities.
/// </summary>
[RequiresPasses(typeof(TerrainGenerationPass))]
[GlobalClass]
public partial class CityGenerationPass : GenerationPass
{
    public override void RunPass(GenerationContext ctx, ChunkBuffer chunk)
    {
        var worldState = ctx.World;

        if (!chunk.HasAnyCity)
        {
            return;
        }

        var terrain = ctx.Definitions.Terrain;
        var tileSize = chunk.Descriptor.ChunkSizeTiles;

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
        ChunkBuffer chunk,
        City city,
        TerrainConfig terrain,
        int tileSize
    )
    {
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

                if (dist <= city.Radius)
                {
                    if (dist <= city.Radius * terrain.CityCenterRatio)
                    {
                        chunk.TerrainData[idx] = terrain.BuildingTerrainId;
                    }
                    else if (IsStreetTile(worldX, worldY, terrain.StreetGridSpacing))
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

    // World tile coordinates, so the grid lines up across chunk borders. A multiple of the
    // spacing has remainder 0 for negatives too.
    private static bool IsStreetTile(int worldX, int worldY, int gridSpacing)
    {
        return worldX % gridSpacing == 0 || worldY % gridSpacing == 0;
    }
}
