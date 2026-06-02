namespace Ouranos.Foundry.Features.ProceduralGeneration.Types;

/// <summary>
/// A road connecting two cities, consisting of path points that define its route
/// through the world terrain.
/// </summary>
public sealed record Road(
    Guid Id,
    Guid SourceCityId,
    Guid TargetCityId,
    string RoadTypeId,
    List<Vector2I> PathPoints
);
