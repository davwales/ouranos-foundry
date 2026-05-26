using System;
using System.Collections.Generic;
using Godot;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Types;

/// <summary>
/// A named geographic region within the world, containing cities and roads.
/// Regions use Voronoi partitioning: every point in the world belongs to its
/// nearest region center, guaranteeing full coverage with no overlap.
/// </summary>
public sealed record Region(
    Guid Id,
    string Name,
    Vector2I Center,
    string BiomeId,
    List<Guid> CityIds,
    List<Guid> RoadIds
);
