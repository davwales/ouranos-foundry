#nullable enable

using Godot;
using Ouranos.Foundry.Core.Attributes;
using Ouranos.Foundry.Features.ProceduralGeneration.Resources;
using Ouranos.Foundry.Features.ProceduralGeneration.Types;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Nodes.Generation;

/// <summary>
/// Records points of interest within chunks that overlap with cities.
/// POIs are already placed during the CityBlueprintPass; this pass
/// marks their positions in chunk data for later scene instantiation.
/// </summary>
[RequiresPasses(typeof(TerrainGenerationPass))]
[GlobalClass]
public partial class PoiPlacementPass : GenerationPass
{
    public override void RunPass(GenerationContext ctx, ChunkBuffer chunk)
    {
        var (definitions, worldState) = (ctx.Definitions, ctx.World);

        if (!chunk.HasAnyCity)
        {
            return;
        }

        var terrain = definitions.Terrain;
        var tileSize = chunk.Descriptor.ChunkSizeTiles;
        var chunkWorldX = chunk.Descriptor.Coordinates.X * tileSize;
        var chunkWorldY = chunk.Descriptor.Coordinates.Y * tileSize;
        var chunkRect = new Rect2I(chunkWorldX, chunkWorldY, tileSize, tileSize);

        foreach (var cityId in chunk.CityIds)
        {
            if (!worldState.CityById.TryGetValue(cityId, out var city))
            {
                continue;
            }

            foreach (var poi in city.PointsOfInterest)
            {
                if (chunkRect.HasPoint(poi.Position))
                {
                    MarkPoiInChunk(
                        chunk,
                        poi,
                        chunkWorldX,
                        chunkWorldY,
                        terrain,
                        definitions,
                        tileSize
                    );
                }
            }
        }
    }

    private static void MarkPoiInChunk(
        ChunkBuffer chunk,
        PointOfInterest poi,
        int chunkWorldX,
        int chunkWorldY,
        TerrainConfig terrain,
        GenerationDefinitions definitions,
        int tileSize
    )
    {
        var poiDef = definitions.GetPoiType(poi.PoiTypeId);
        var localX = poi.Position.X - chunkWorldX;
        var localY = poi.Position.Y - chunkWorldY;

        if (localX < 0 || localX >= tileSize || localY < 0 || localY >= tileSize)
        {
            return;
        }

        chunk.SetTile(localX, localY, terrain.PoiTerrainIdBase + poiDef.TerrainIdOffset);
    }
}
