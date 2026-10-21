#nullable enable

using Godot;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Resources;

/// <summary>
/// Single source of truth for chunk dimensions. Assign it on the
/// <c>ProceduralGenerationConfig</c> used by <c>WorldGenerator</c>, which pushes the
/// size into <c>WorldState</c>, <c>ChunkManager</c> and every chunk's descriptor.
/// </summary>
[GlobalClass]
public partial class ChunkConfig : Resource
{
    /// <summary>Width and height of a chunk, in tiles.</summary>
    [Export]
    public int ChunkSizeTiles { get; set; } = 16;

    /// <summary>
    /// Width and height of a tile, in world units (pixels in 2D). <c>ChunkManager</c>
    /// uses it to convert the player's position to a chunk.
    /// </summary>
    [Export]
    public int TilePixelSize { get; set; } = 1;

    [Export]
    public int ViewDistance { get; set; } = 3;
}
