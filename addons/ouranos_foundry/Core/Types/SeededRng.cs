#nullable enable

using System.Collections.Generic;

namespace Ouranos.Foundry.Core.Types;

/// <summary>
/// Provides deterministic random number generation from a world seed and
/// optional salt values. Every call with the same seed+salt produces the
/// same sequence, enabling reproducible world generation.
/// Uses SplitMix64 internally for bit-identical output across all platforms
/// and .NET versions.
/// </summary>
public sealed class SeededRng(ulong seed, ulong salt = 0)
{
    private ulong _state = HashCombine(seed, salt);
    private const ulong SplitMix64Increment = 0x9e3779b97f4a7c15;
    private const ulong SplitMix64Multiplier1 = 0xbf58476d1ce4e5b9;
    private const ulong SplitMix64Multiplier2 = 0x94d049bb133111eb;
    private const int SplitMix64Shift1 = 30;
    private const int SplitMix64Shift2 = 27;
    private const int SplitMix64Shift3 = 31;
    private const ulong HashCombineMixer = 0x9e3779b9;
    private const int NextOutputShift = 33;
    private const int DoubleOutputShift = 11;
    private const double DoubleNormalizationFactor = 1.1102230246251565e-16;
    private const float FloatBelowOne = 0.99999994f;

    /// <summary>
    /// Returns a non-negative random integer.
    /// </summary>
    public int Next()
    {
        return (int)(SplitMix64Next(ref _state) >> NextOutputShift);
    }

    /// <summary>
    /// Returns a non-negative random integer less than maxValue, without modulo bias.
    /// </summary>
    public int Next(int maxValue)
    {
        if (maxValue < 1)
        {
            throw new System.ArgumentOutOfRangeException(
                nameof(maxValue),
                "maxValue must be greater than 0."
            );
        }

        return (int)NextBelow((ulong)maxValue);
    }

    /// <summary>
    /// Returns a random integer in [minValue, maxValue), without modulo bias. Any range is
    /// supported, including <c>int.MinValue..int.MaxValue</c>. Like <c>System.Random</c>,
    /// returns <paramref name="minValue"/> when the range is empty (<c>minValue == maxValue</c>),
    /// without advancing the stream, and throws when <c>minValue &gt; maxValue</c>.
    /// </summary>
    public int Next(int minValue, int maxValue)
    {
        if (minValue > maxValue)
        {
            throw new System.ArgumentOutOfRangeException(
                nameof(minValue),
                "minValue must not be greater than maxValue."
            );
        }

        if (minValue == maxValue)
        {
            return minValue;
        }

        var range = (ulong)((long)maxValue - minValue);
        return (int)(minValue + (long)NextBelow(range));
    }

    /// <summary>
    /// Returns a random integer in [minValue, maxValue], both ends inclusive, without
    /// modulo bias. Any range is supported, including <c>int.MinValue..int.MaxValue</c>.
    /// </summary>
    public int NextInclusive(int minValue, int maxValue)
    {
        if (minValue > maxValue)
        {
            throw new System.ArgumentOutOfRangeException(
                nameof(maxValue),
                "maxValue must be greater than or equal to minValue."
            );
        }

        var range = (ulong)((long)maxValue - minValue) + 1;
        return (int)(minValue + (long)NextBelow(range));
    }

    /// <summary>
    /// Returns true or false with equal probability.
    /// </summary>
    public bool NextBool()
    {
        return (SplitMix64Next(ref _state) >> 63) != 0;
    }

    /// <summary>
    /// Returns a random double in [0.0, 1.0).
    /// </summary>
    public double NextDouble()
    {
        return (SplitMix64Next(ref _state) >> DoubleOutputShift) * DoubleNormalizationFactor;
    }

    /// <summary>
    /// Returns a random float in [0.0, 1.0).
    /// </summary>
    public float NextFloat()
    {
        // Narrowing can round values just below 1.0 up to 1.0f, so clamp to keep [0, 1).
        return System.MathF.Min((float)NextDouble(), FloatBelowOne);
    }

    /// <summary>
    /// Returns a random float in [minValue, maxValue).
    /// </summary>
    public float NextFloat(float minValue, float maxValue)
    {
        if (!(minValue < maxValue) || float.IsInfinity(maxValue - minValue))
        {
            throw new System.ArgumentOutOfRangeException(
                nameof(maxValue),
                "maxValue must be greater than minValue, and the range must be finite."
            );
        }

        var value = (float)(minValue + NextDouble() * ((double)maxValue - minValue));
        return value < maxValue ? value : System.MathF.BitDecrement(maxValue);
    }

    /// <summary>
    /// Returns a random vector within the specified rectangle bounds.
    /// </summary>
    public Godot.Vector2I NextVector2I(Godot.Rect2I bounds)
    {
        var x = Next(bounds.Position.X, bounds.Position.X + bounds.Size.X);
        var y = Next(bounds.Position.Y, bounds.Position.Y + bounds.Size.Y);
        return new Godot.Vector2I(x, y);
    }

    /// <summary>
    /// Returns a random element from the provided array.
    /// </summary>
    public T NextElement<T>(T[] array)
    {
        if (array.Length == 0)
        {
            throw new System.ArgumentException("Array must not be empty.", nameof(array));
        }

        return array[Next(array.Length)];
    }

    /// <summary>
    /// Returns a random element from the provided list.
    /// </summary>
    public T NextElement<T>(IList<T> list)
    {
        if (list.Count == 0)
        {
            throw new System.ArgumentException("List must not be empty.", nameof(list));
        }

        return list[Next(list.Count)];
    }

    /// <summary>
    /// Shuffles the list in place (Fisher-Yates).
    /// </summary>
    public void Shuffle<T>(IList<T> list)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    /// <summary>
    /// Creates a derived RNG for a specific generation pass or entity.
    /// Uses the current RNG to produce a new seed, ensuring determinism
    /// while allowing independent RNG streams per pass.
    /// </summary>
    public SeededRng Derive()
    {
        var derivedSeed = SplitMix64Next(ref _state);
        return new SeededRng(derivedSeed);
    }

    /// <summary>
    /// Creates a stream keyed by a 2D coordinate, such as a chunk or tile, so each
    /// coordinate gets its own stream for a given seed and salt. For a fixed
    /// <paramref name="seed"/> and <paramref name="salt"/>, distinct coordinates (negatives
    /// included) always give distinct streams: both coordinates are packed into one 64-bit
    /// key and mixed with a bijection before being combined with the seed and salt.
    /// </summary>
    public static SeededRng ForCoords(ulong seed, int x, int y, ulong salt = 0)
    {
        var key = ((ulong)(uint)x << 32) | (uint)y;
        return new SeededRng(HashCombine(seed, salt), Mix(key));
    }

    /// <summary>
    /// Hashes a single value with the SplitMix64 mixer. Stateless and deterministic.
    /// </summary>
    internal static ulong Mix(ulong value)
    {
        return SplitMix64Next(ref value);
    }

    /// <summary>
    /// Lemire's nearly divisionless method: returns a uniform value in [0, range).
    /// Multiplies a 64-bit draw by the range and keeps the high word, rejecting the
    /// few low words that would bias the result.
    /// </summary>
    private ulong NextBelow(ulong range)
    {
        var high = System.Math.BigMul(SplitMix64Next(ref _state), range, out var low);
        if (low < range)
        {
            var threshold = (0 - range) % range;
            while (low < threshold)
            {
                high = System.Math.BigMul(SplitMix64Next(ref _state), range, out low);
            }
        }

        return high;
    }

    private static ulong SplitMix64Next(ref ulong state)
    {
        state += SplitMix64Increment;
        ulong z = state;
        z = (z ^ (z >> SplitMix64Shift1)) * SplitMix64Multiplier1;
        z = (z ^ (z >> SplitMix64Shift2)) * SplitMix64Multiplier2;
        return z ^ (z >> SplitMix64Shift3);
    }

    private static ulong HashCombine(ulong a, ulong b)
    {
        var hash = a;
        hash ^= b + HashCombineMixer + (hash << 6) + (hash >> 2);
        return hash;
    }
}
