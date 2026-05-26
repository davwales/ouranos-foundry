using System;
using System.Collections.Generic;
using Godot;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Types;

/// <summary>
/// A settled area within a region, containing buildings, points of interest,
/// and acting as a hub for roads and player activities.
/// </summary>
public sealed record City(
    Guid Id,
    string Name,
    Vector2I Position,
    string SizeId,
    Guid RegionId,
    List<PointOfInterest> PointsOfInterest,
    List<Guid> RoadIds,
    int Radius
);
