using System;
using System.Collections.Generic;
using Godot;
using Ouranos.Foundry.Features.ProceduralGeneration.Types;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Resources;

/// <summary>
/// Immutable descriptor populated by WorldGenerator before generation passes run.
/// Contains all the information a generation pass needs about a chunk's identity
/// and overlapping entities.
/// </summary>
public sealed record ChunkDescriptor(
    Vector2I Coordinates,
    ChunkType Type,
    IReadOnlySet<Guid> OverlappingCityIds,
    IReadOnlySet<Guid> OverlappingRoadIds,
    int TileSize = 16
);
