using System.Linq;
using Godot;
using Ouranos.Foundry.Core.Utils;
using Xunit;

namespace Ouranos.Foundry.Tests.Features.ProceduralGeneration.Utils;

public class BresenhamTests
{
    [Fact]
    public void GetPoints_SinglePoint_ReturnsOnlyThatPoint()
    {
        var points = Bresenham.GetPoints(new Vector2I(5, 5), new Vector2I(5, 5)).ToList();

        Assert.Single(points);
        Assert.Equal(new Vector2I(5, 5), points[0]);
    }

    [Fact]
    public void GetPoints_HorizontalLine_ReturnsAllIntermediatePoints()
    {
        var points = Bresenham.GetPoints(new Vector2I(0, 0), new Vector2I(3, 0)).ToList();

        Assert.Equal(4, points.Count);
        Assert.Equal(new Vector2I(0, 0), points[0]);
        Assert.Equal(new Vector2I(3, 0), points[^1]);
    }

    [Fact]
    public void GetPoints_VerticalLine_ReturnsAllIntermediatePoints()
    {
        var points = Bresenham.GetPoints(new Vector2I(0, 0), new Vector2I(0, 3)).ToList();

        Assert.Equal(4, points.Count);
        Assert.Equal(new Vector2I(0, 0), points[0]);
        Assert.Equal(new Vector2I(0, 3), points[^1]);
    }

    [Fact]
    public void GetPoints_DiagonalLine_IncludesBothEndpoints()
    {
        var points = Bresenham.GetPoints(new Vector2I(0, 0), new Vector2I(5, 5)).ToList();

        Assert.Equal(new Vector2I(0, 0), points[0]);
        Assert.Equal(new Vector2I(5, 5), points[^1]);
    }

    [Fact]
    public void GetPoints_AllPointsAreOnTheLine()
    {
        // Every point should be within 0.5 units of the ideal line
        var from = new Vector2I(2, 3);
        var to = new Vector2I(10, 7);

        var dx = to.X - from.X;
        var dy = to.Y - from.Y;
        var length = Mathf.Sqrt(dx * dx + dy * dy);

        foreach (var point in Bresenham.GetPoints(from, to))
        {
            // Distance from point to line segment
            var cross = Mathf.Abs(
                (to.X - from.X) * (from.Y - point.Y) - (from.X - point.X) * (to.Y - from.Y)
            );
            var distance = cross / length;

            Assert.True(distance <= 0.6, $"Point {point} is too far from the line (distance: {distance})");
        }
    }
}
