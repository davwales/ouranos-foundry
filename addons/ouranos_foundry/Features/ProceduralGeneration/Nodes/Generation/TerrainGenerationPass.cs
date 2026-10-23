#nullable enable

using Godot;
using Ouranos.Foundry.Core.Types;
using Ouranos.Foundry.Features.ProceduralGeneration.Types;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Nodes.Generation;

/// <summary>
/// Generates base terrain for a chunk based on its region's biome.
/// This is a pure-data pass that populates ChunkBuffer.TerrainData without
/// touching the scene tree, making it safe for off-thread execution.
/// </summary>
[GlobalClass]
public partial class TerrainGenerationPass : GenerationPass
{
    public override void RunPass(GenerationContext ctx, ChunkBuffer chunk)
    {
        var (definitions, worldState) = (ctx.Definitions, ctx.World);
        var terrain = definitions.Terrain;
        var tileSize = chunk.Descriptor.ChunkSizeTiles;

        // A stream per chunk rather than ctx.Rng, which is the same for every chunk.
        var rng = new SeededRng(
            worldState.Seed,
            (ulong)(
                chunk.Descriptor.Coordinates.X * 73856093
                + chunk.Descriptor.Coordinates.Y * 19349663
            )
        );

        var worldX = chunk.Descriptor.Coordinates.X * tileSize;
        var worldY = chunk.Descriptor.Coordinates.Y * tileSize;
        var chunkCenter = new Vector2I(worldX + tileSize / 2, worldY + tileSize / 2);

        var region = worldState.FindRegionAt(chunkCenter);
        int baseTerrain;
        int variantCount;

        if (region is not null)
        {
            var biomeDef = definitions.GetBiome(region.BiomeId);
            baseTerrain = biomeDef.TerrainId;
            variantCount = biomeDef.TerrainVariantCount;
        }
        else
        {
            baseTerrain = terrain.DefaultTerrainId;
            variantCount = 3;
        }

        for (var i = 0; i < chunk.TerrainData.Length; i++)
        {
            if (rng.NextDouble() < terrain.TerrainNoiseChance)
            {
                chunk.TerrainData[i] = baseTerrain + rng.Next(1, variantCount + 1);
            }
            else
            {
                chunk.TerrainData[i] = baseTerrain;
            }
        }
    }
}
