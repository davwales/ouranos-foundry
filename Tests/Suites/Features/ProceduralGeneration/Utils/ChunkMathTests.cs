using GdUnit4;
using Godot;
using Ouranos.Foundry.Features.ProceduralGeneration.Utils;
using Shouldly;

namespace Ouranos.Foundry.Tests.Features.ProceduralGeneration.Utils;

[TestSuite]
public class ChunkMathTests
{
    [TestCase(0, 16, 0)]
    [TestCase(15, 16, 0)]
    [TestCase(16, 16, 1)]
    [TestCase(-1, 16, -1)]
    [TestCase(-16, 16, -1)]
    [TestCase(-17, 16, -2)]
    [TestCase(0, 32, 0)]
    [TestCase(15, 32, 0)]
    [TestCase(16, 32, 0)]
    [TestCase(-1, 32, -1)]
    [TestCase(-16, 32, -1)]
    [TestCase(-17, 32, -1)]
    [TestCase(-32, 32, -1)]
    [TestCase(-33, 32, -2)]
    public void WorldToChunk_ForTile_ReturnsFlooredChunk(int tile, int chunkSizeTiles, int expected)
    {
        // Act
        var chunk = ChunkMath.WorldToChunk(new Vector2I(tile, tile), chunkSizeTiles);

        // Assert
        chunk.ShouldBe(new Vector2I(expected, expected));
    }

    [TestCase(0f, 0)]
    [TestCase(255.9f, 0)]
    [TestCase(256f, 1)]
    [TestCase(-0.5f, -1)]
    [TestCase(-256f, -1)]
    [TestCase(-256.5f, -2)]
    public void PixelToChunk_ForPosition_ReturnsFlooredChunk(float pixel, int expected)
    {
        // Act
        var chunk = ChunkMath.PixelToChunk(new Vector2(pixel, pixel), 256);

        // Assert
        chunk.ShouldBe(new Vector2I(expected, expected));
    }

    [TestCase(7, 2, 3)]
    [TestCase(-7, 2, -4)]
    [TestCase(-8, 2, -4)]
    public void FloorDiv_ForValue_RoundsTowardNegativeInfinity(int value, int divisor, int expected)
    {
        // Act & Assert
        ChunkMath.FloorDiv(value, divisor).ShouldBe(expected);
    }
}
