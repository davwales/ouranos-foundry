#nullable enable

using Godot;
using Ouranos.Foundry.Core.Types;
using Ouranos.Foundry.Core.Utils;

namespace Ouranos.Foundry.Core.Resources;

/// <summary>
/// A seeded, deterministic random stream that can be authored in the inspector and
/// called from GDScript. Wraps <see cref="SeededRng"/>; C# code can use <see cref="Rng"/>
/// directly to pass the stream to APIs that take a <see cref="SeededRng"/>.
/// The stream is stateful: a resource shared by several nodes is one shared sequence.
/// Enable <c>resource_local_to_scene</c> or duplicate it for independent streams.
/// </summary>
[GlobalClass]
public partial class FoundryRandom : Resource
{
    private SeededRng? _rng;

    [Export]
    public long Seed
    {
        get;
        set
        {
            field = value;
            Reset();
        }
    }

    [Export]
    public long Salt
    {
        get;
        set
        {
            field = value;
            Reset();
        }
    }

    /// <summary>
    /// The underlying stream. Created on first use and recreated after <see cref="Reset"/>.
    /// </summary>
    public SeededRng Rng => _rng ??= new SeededRng((ulong)Seed, (ulong)Salt);

    /// <summary>
    /// Restarts the sequence from the beginning for the current seed and salt.
    /// </summary>
    public void Reset()
    {
        _rng = null;
    }

    /// <summary>
    /// Returns a float in [0, 1).
    /// </summary>
    public float Randf()
    {
        return Rng.NextFloat();
    }

    /// <summary>
    /// Returns a float in [from, to). The bounds may be given in either order;
    /// equal bounds return that value.
    /// </summary>
    public float RandfRange(float from, float to)
    {
        if (from == to)
        {
            return from;
        }

        return from < to ? Rng.NextFloat(from, to) : Rng.NextFloat(to, from);
    }

    /// <summary>
    /// Returns an integer in [from, to], both inclusive, like
    /// <c>RandomNumberGenerator.randi_range</c>. The bounds may be given in either order.
    /// </summary>
    public int RandiRange(int from, int to)
    {
        return from <= to ? Rng.NextInclusive(from, to) : Rng.NextInclusive(to, from);
    }

    /// <summary>
    /// Returns true or false with equal probability.
    /// </summary>
    public bool RandBool()
    {
        return Rng.NextBool();
    }

    /// <summary>
    /// Returns a weighted random index into <paramref name="weights"/>, like
    /// <c>RandomNumberGenerator.rand_weighted</c>, or -1 if no weight is positive.
    /// </summary>
    public int RandWeighted(float[] weights)
    {
        return WeightedRandom.PickIndex(weights, Rng);
    }

    /// <summary>
    /// Shuffles the array in place.
    /// </summary>
    public void Shuffle(Godot.Collections.Array array)
    {
        Rng.Shuffle(array);
    }
}
