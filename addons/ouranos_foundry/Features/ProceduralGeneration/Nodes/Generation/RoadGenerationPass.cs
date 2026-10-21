#nullable enable

using Godot;
using Ouranos.Foundry.Core.Attributes;
using Ouranos.Foundry.Core.Utils;
using Ouranos.Foundry.Features.ProceduralGeneration.Types;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Nodes.Generation;

/// <summary>
/// Generates road tiles within chunks that overlap with road path points.
/// Draws road paths between waypoints using Bresenham-style line traversal.
/// </summary>
[RequiresPasses(typeof(TerrainGenerationPass))]
[GlobalClass]
public partial class RoadGenerationPass : GenerationPass
{
    public override void RunPass(GenerationContext ctx, ChunkBuffer chunk)
    {
        var (definitions, worldState) = (ctx.Definitions, ctx.World);

        if (!chunk.HasAnyRoad)
        {
            return;
        }

        var tileSize = chunk.Descriptor.ChunkSizeTiles;

        foreach (var roadId in chunk.RoadIds)
        {
            if (!worldState.RoadById.TryGetValue(roadId, out var road))
            {
                continue;
            }

            OverlayRoadOnChunk(chunk, road, definitions, tileSize);
        }
    }

    private static void OverlayRoadOnChunk(
        ChunkBuffer chunk,
        Road road,
        GenerationDefinitions definitions,
        int tileSize
    )
    {
        if (chunk.TerrainData is null || chunk.TerrainData.Length == 0)
        {
            return;
        }

        var chunkWorldX = chunk.Descriptor.Coordinates.X * tileSize;
        var chunkWorldY = chunk.Descriptor.Coordinates.Y * tileSize;
        var roadDef = definitions.GetRoadType(road.RoadTypeId);

        for (var i = 0; i < road.PathPoints.Count - 1; i++)
        {
            var from = road.PathPoints[i];
            var to = road.PathPoints[i + 1];
            DrawRoadSegment(chunk, from, to, chunkWorldX, chunkWorldY, roadDef.TerrainId, tileSize);
        }
    }

    private static void DrawRoadSegment(
        ChunkBuffer chunk,
        Vector2I from,
        Vector2I to,
        int chunkWorldX,
        int chunkWorldY,
        int terrainId,
        int tileSize
    )
    {
        foreach (var point in Bresenham.GetPoints(from, to))
        {
            var localX = point.X - chunkWorldX;
            var localY = point.Y - chunkWorldY;

            if (localX >= 0 && localX < tileSize && localY >= 0 && localY < tileSize)
            {
                var idx = localY * tileSize + localX;
                if (idx >= 0 && idx < chunk.TerrainData.Length)
                {
                    chunk.TerrainData[idx] = terrainId;
                }
            }
        }
    }
}
