#nullable enable

using Godot;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Utils;

/// <summary>
/// Converts world coordinates to chunk coordinates. Every conversion floors rather
/// than truncating toward zero, so negative positions land in negative chunks:
/// with 16-tile chunks, tiles -16..-1 are chunk -1 and tiles 0..15 are chunk 0.
/// </summary>
public static class ChunkMath
{
    /// <summary>
    /// Integer division rounded toward negative infinity (C#'s <c>/</c> rounds toward zero).
    /// <paramref name="divisor"/> must be positive.
    /// </summary>
    public static int FloorDiv(int value, int divisor)
    {
        var quotient = value / divisor;
        return value % divisor < 0 ? quotient - 1 : quotient;
    }

    /// <summary>
    /// Returns the chunk containing the given tile.
    /// </summary>
    public static Vector2I WorldToChunk(Vector2I tile, int chunkSizeTiles)
    {
        return new Vector2I(FloorDiv(tile.X, chunkSizeTiles), FloorDiv(tile.Y, chunkSizeTiles));
    }

    /// <summary>
    /// Returns the chunk containing the given world-space (pixel) position.
    /// </summary>
    public static Vector2I PixelToChunk(Vector2 position, int chunkSizePixels)
    {
        return new Vector2I(
            Mathf.FloorToInt(position.X / chunkSizePixels),
            Mathf.FloorToInt(position.Y / chunkSizePixels)
        );
    }
}
