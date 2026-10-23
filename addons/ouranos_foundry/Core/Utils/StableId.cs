#nullable enable

using System;
using System.Buffers.Binary;
using Ouranos.Foundry.Core.Types;

namespace Ouranos.Foundry.Core.Utils;

/// <summary>
/// Derives deterministic <see cref="Guid"/> identifiers from a world seed or a
/// parent ID, so the same seed always produces the same IDs. IDs are a pure
/// function of their inputs and never consume an RNG stream, so they stay stable
/// regardless of the order in which generation passes draw random numbers.
/// </summary>
public static class StableId
{
    private const ulong SecondHalfSalt = 0xd6e8feb86659fd93;
    private const ulong FnvOffsetBasis = 0xcbf29ce484222325;
    private const ulong FnvPrime = 0x100000001b3;

    /// <summary>
    /// Creates an ID for the <paramref name="index"/>-th entity of
    /// <paramref name="kind"/> in the world generated from <paramref name="seed"/>.
    /// </summary>
    public static Guid Create(ulong seed, string kind, ulong index)
    {
        var state = SeededRng.Mix(SeededRng.Mix(seed) ^ Hash(kind));
        return FromState(SeededRng.Mix(state ^ index));
    }

    /// <summary>
    /// Creates an ID for the <paramref name="index"/>-th child of
    /// <paramref name="kind"/> owned by <paramref name="parent"/>.
    /// </summary>
    public static Guid Create(Guid parent, string kind, ulong index)
    {
        return Create(Fold(parent), kind, index);
    }

    /// <summary>
    /// Creates an ID for an entity of <paramref name="kind"/> that links
    /// <paramref name="first"/> to <paramref name="second"/>. Order matters.
    /// </summary>
    public static Guid Create(Guid first, string kind, Guid second)
    {
        return Create(Fold(first), kind, Fold(second));
    }

    private static Guid FromState(ulong state)
    {
        Span<byte> bytes = stackalloc byte[16];
        BinaryPrimitives.WriteUInt64LittleEndian(bytes, state);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes[8..], SeededRng.Mix(state ^ SecondHalfSalt));
        return new Guid(bytes);
    }

    private static ulong Fold(Guid id)
    {
        Span<byte> bytes = stackalloc byte[16];
        id.TryWriteBytes(bytes);
        var low = BinaryPrimitives.ReadUInt64LittleEndian(bytes);
        var high = BinaryPrimitives.ReadUInt64LittleEndian(bytes[8..]);
        return SeededRng.Mix(SeededRng.Mix(low) ^ high);
    }

    /// <summary>
    /// Hashes a string with 64-bit FNV-1a. Unlike <c>string.GetHashCode()</c>, which is
    /// randomized per process, the result is the same on every run and platform.
    /// </summary>
    public static ulong Hash(string value)
    {
        var hash = FnvOffsetBasis;
        foreach (var c in value)
        {
            hash = (hash ^ c) * FnvPrime;
        }

        return hash;
    }
}
