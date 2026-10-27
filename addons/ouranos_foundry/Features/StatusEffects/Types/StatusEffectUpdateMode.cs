#nullable enable

namespace Ouranos.Foundry.Features.StatusEffects.Types;

/// <summary>
/// What advances a <c>StatusEffectComponent</c>'s timers.
/// </summary>
public enum StatusEffectUpdateMode
{
    /// <summary>Every frame, by <c>_Process</c> delta (seconds).</summary>
    Process,

    /// <summary>Every physics tick, by <c>_PhysicsProcess</c> delta (seconds).</summary>
    Physics,

    /// <summary>
    /// Only when you call <c>Advance</c>. Durations are in whatever unit you advance by,
    /// e.g. <c>Advance(1)</c> once per turn.
    /// </summary>
    Manual,
}
