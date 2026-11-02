#nullable enable

namespace Ouranos.Foundry.Features.StatusEffects.Types;

public enum StatusEffectDurationType
{
    /// <summary>Lasts <c>Duration</c>, then expires.</summary>
    Timed,

    /// <summary>Lasts until removed.</summary>
    Infinite,

    /// <summary>Applied and removed in the same call; never listed as active.</summary>
    Instant,
}
