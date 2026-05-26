using System;
using Godot;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Types;

/// <summary>
/// A notable location within a city that the player can visit and interact with.
/// POIs are placed during city generation and may serve as quest targets.
/// </summary>
public sealed record PointOfInterest(
    Guid Id,
    string Name,
    string PoiTypeId,
    Vector2I Position,
    Guid CityId
);
