#nullable enable

using System;
using System.Collections.Generic;
using Ouranos.Foundry.Core.Types;

namespace Ouranos.Foundry.Core.Utils;

/// <summary>
/// Weighted random selection. Each item is picked with probability
/// <c>weight / totalWeight</c>; zero, negative, NaN and infinite weights count as zero.
/// Draws exactly one value from the RNG when anything is pickable, and none otherwise.
/// </summary>
public static class WeightedRandom
{
    /// <summary>
    /// Returns the picked item, or <c>default</c> when no item has a positive weight.
    /// </summary>
    public static T? Pick<T>(IReadOnlyList<T> items, Func<T, float> weightOf, SeededRng rng)
    {
        var index = PickIndex(items, weightOf, rng);
        return index < 0 ? default : items[index];
    }

    /// <summary>
    /// Returns the index of the picked item, or -1 when no item has a positive weight.
    /// </summary>
    public static int PickIndex<T>(IReadOnlyList<T> items, Func<T, float> weightOf, SeededRng rng)
    {
        var weights = new float[items.Count];
        for (var i = 0; i < items.Count; i++)
        {
            weights[i] = weightOf(items[i]);
        }

        return PickIndex(weights, rng);
    }

    /// <summary>
    /// Returns the index of the picked weight, or -1 when no weight is positive.
    /// </summary>
    public static int PickIndex(IReadOnlyList<float> weights, SeededRng rng)
    {
        var total = 0d;
        var lastPickable = -1;
        for (var i = 0; i < weights.Count; i++)
        {
            if (IsPickable(weights[i]))
            {
                total += weights[i];
                lastPickable = i;
            }
        }

        if (lastPickable < 0)
        {
            return -1;
        }

        var roll = rng.NextDouble() * total;
        var cumulative = 0d;
        for (var i = 0; i < lastPickable; i++)
        {
            if (!IsPickable(weights[i]))
            {
                continue;
            }

            cumulative += weights[i];
            if (roll < cumulative)
            {
                return i;
            }
        }

        // Also absorbs floating-point drift at the top of the range.
        return lastPickable;
    }

    private static bool IsPickable(float weight)
    {
        return weight > 0f && float.IsFinite(weight);
    }
}
