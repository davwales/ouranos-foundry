#nullable enable

using System.Collections.Generic;
using System.Linq;
using Godot;
using Ouranos.Foundry.Features.ProceduralGeneration.Utils;

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

    /// <summary>
    /// Returns a message for each invalid size. Empty when the config is valid.
    /// </summary>
    public virtual IEnumerable<string> Validate()
    {
        const string owner = nameof(ChunkConfig);
        string?[] messages =
        [
            ConfigValidation.AtLeast(owner, nameof(ChunkSizeTiles), ChunkSizeTiles, 1),
            ConfigValidation.AtLeast(owner, nameof(TilePixelSize), TilePixelSize, 1),
            ConfigValidation.AtLeast(owner, nameof(ViewDistance), ViewDistance, 0),
        ];
        return messages.OfType<string>();
    }
}
