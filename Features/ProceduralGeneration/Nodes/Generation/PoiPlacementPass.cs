#nullable enable

using Godot;
using Ouranos.Foundry.Core.Attributes;
using Ouranos.Foundry.Features.ProceduralGeneration.Types;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Nodes.Generation;

/// <summary>
/// Marks each of the chunk's points of interest (<c>ChunkBuffer.PointsOfInterest</c>) in its
/// terrain with the POI type's tile id. POIs are placed by <c>CityBlueprintPass</c>; to
/// instantiate a scene per POI, read <c>ChunkData.PointsOfInterest</c>.
/// </summary>
[RequiresPasses(typeof(TerrainGenerationPass))]
[GlobalClass]
public partial class PoiPlacementPass : GenerationPass
{
    public override void RunPass(GenerationContext ctx, ChunkBuffer chunk)
    {
        var terrain = ctx.Definitions.Terrain;
        var origin = chunk.Coordinates * chunk.Descriptor.ChunkSizeTiles;

        foreach (var poi in chunk.PointsOfInterest)
        {
            var local = poi.Position - origin;
            var poiDef = ctx.Definitions.GetPoiType(poi.PoiTypeId);
            chunk.SetTile(local.X, local.Y, terrain.PoiTerrainIdBase + poiDef.TerrainIdOffset);
        }
    }
}
