using System;
using System.Collections.Generic;
using Godot;
using Ouranos.Foundry.Features.ProceduralGeneration.Resources;
using Ouranos.Foundry.Features.ProceduralGeneration.Types;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Nodes.Generation;

/// <summary>
/// Records points of interest within chunks that overlap with cities.
/// POIs are already placed during the CityBlueprintPass; this pass
/// marks their positions in chunk data for later scene instantiation.
/// </summary>
[GlobalClass]
public partial class PoiPlacementPass : GenerationPass
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
        var chunkConfig = config.ChunkConfig;
        var tileSize = chunkConfig?.TileSize ?? chunk.Descriptor.TileSize;
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
                    MarkPoiInChunk(chunk, poi, chunkWorldX, chunkWorldY, terrain, config, tileSize);
                }
            }
        }
    }

    private static void MarkPoiInChunk(
        ChunkData chunk,
        PointOfInterest poi,
        int chunkWorldX,
        int chunkWorldY,
        TerrainConfig terrain,
        ProceduralGenerationConfig config,
        int tileSize
    )
    {
        if (chunk.TerrainData is null || chunk.TerrainData.Length == 0)
        {
            return;
        }

        var poiDef = config.GetPoiTypeDefinition(poi.PoiTypeId);
        var localX = poi.Position.X - chunkWorldX;
        var localY = poi.Position.Y - chunkWorldY;

        if (localX < 0 || localX >= tileSize || localY < 0 || localY >= tileSize)
        {
            return;
        }

        var idx = localY * tileSize + localX;
        if (idx >= 0 && idx < chunk.TerrainData.Length)
        {
            chunk.TerrainData[idx] = terrain.PoiTerrainIdBase + poiDef.TerrainIdOffset;
        }
    }
}
