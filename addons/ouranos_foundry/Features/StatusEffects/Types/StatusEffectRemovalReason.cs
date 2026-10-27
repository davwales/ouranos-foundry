#nullable enable

namespace Ouranos.Foundry.Features.StatusEffects.Types;

public enum StatusEffectRemovalReason
{
    /// <summary>The duration ran out, or the effect was <c>Instant</c>.</summary>
    Expired,

    /// <summary><c>RemoveEffect</c> or <c>ClearEffects</c> was called.</summary>
    Removed,
}
