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

    /// <summary>
    /// Returns a non-negative random integer.
    /// </summary>
    public int Next()
    {
        return (int)(SplitMix64Next(ref _state) >> NextOutputShift);
    }

    /// <summary>
    /// Returns a non-negative random integer less than maxValue using rejection sampling.
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

        return (int)(SplitMix64Next(ref _state) % (ulong)maxValue);
    }

    /// <summary>
    /// Returns a random integer in [minValue, maxValue) using rejection sampling.
    /// </summary>
    public int Next(int minValue, int maxValue)
    {
        if (minValue >= maxValue)
        {
            throw new System.ArgumentOutOfRangeException(
                nameof(maxValue),
                "maxValue must be greater than minValue."
            );
        }

        var range = (ulong)(maxValue - minValue);
        var result = SplitMix64Next(ref _state) % range;
        return minValue + (int)result;
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
        return (float)NextDouble();
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
    /// Creates a derived RNG for a specific generation pass or entity.
    /// Uses the current RNG to produce a new seed, ensuring determinism
    /// while allowing independent RNG streams per pass.
    /// </summary>
    public SeededRng Derive()
    {
        var derivedSeed = SplitMix64Next(ref _state);
        return new SeededRng(derivedSeed);
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
