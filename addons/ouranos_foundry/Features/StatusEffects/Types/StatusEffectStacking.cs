#nullable enable

namespace Ouranos.Foundry.Features.StatusEffects.Types;

/// <summary>
/// What happens when an effect is applied while one with the same id is already active.
/// </summary>
public enum StatusEffectStacking
{
    /// <summary>Restart the duration.</summary>
    Refresh,

    /// <summary>Add a stack (up to <c>MaxStacks</c>) and restart the duration.</summary>
    StackIntensity,

    /// <summary>Add another <c>Duration</c> to the remaining time.</summary>
    StackDuration,

    /// <summary>Keep the active effect unchanged.</summary>
    Ignore,
}
