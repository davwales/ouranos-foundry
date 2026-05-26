using System.Collections.Generic;
using Godot;

namespace Ouranos.Foundry.Core.Utils;

/// <summary>
/// Provides Bresenham line algorithm utilities for enumerating grid points
/// along a line segment between two coordinates.
/// </summary>
public static class Bresenham
{
    /// <summary>
    /// Enumerates all points along a Bresenham line from <paramref name="from"/> to
    /// <paramref name="to"/>, inclusive of both endpoints.
    /// </summary>
    public static IEnumerable<Vector2I> GetPoints(Vector2I from, Vector2I to)
    {
        var x0 = from.X;
        var y0 = from.Y;
        var x1 = to.X;
        var y1 = to.Y;

        var dx = System.Math.Abs(x1 - x0);
        var dy = System.Math.Abs(y1 - y0);
        var sx = x0 < x1 ? 1 : -1;
        var sy = y0 < y1 ? 1 : -1;
        var err = dx - dy;

        while (true)
        {
            yield return new Vector2I(x0, y0);

            if (x0 == x1 && y0 == y1)
            {
                yield break;
            }

            var e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }

            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }
}
