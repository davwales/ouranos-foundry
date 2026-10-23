#nullable enable

using Godot;
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
    public override ulong RngSalt => 4000;

    public override void RunPass(GenerationContext ctx, ChunkBuffer chunk)
    {
        var (definitions, worldState, rng) = (ctx.Definitions, ctx.World, ctx.Rng);
        var terrain = definitions.Terrain;
        var tileSize = chunk.Descriptor.ChunkSizeTiles;

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
            // A biome with no variants has no noise.
            if (variantCount > 0 && rng.NextDouble() < terrain.TerrainNoiseChance)
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
