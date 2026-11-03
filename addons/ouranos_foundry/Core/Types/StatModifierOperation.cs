#nullable enable

namespace Ouranos.Foundry.Core.Types;

/// <summary>
/// How a <c>StatModifier</c> changes a stat. A stat's value is
/// <c>(base + Σ Add) × (1 + Σ PercentAdd) × Π Multiply</c>, unless an <c>Override</c> applies.
/// </summary>
public enum StatModifierOperation
{
    /// <summary>Adds <c>Value</c> to the base value.</summary>
    Add,

    /// <summary>
    /// Adds <c>Value</c> as a fraction of the flat total: 0.1 is +10%. Percentages add up,
    /// so two +10% modifiers give +20%.
    /// </summary>
    PercentAdd,

    /// <summary>Multiplies the result by <c>Value</c>: 1.5 is ×1.5. Multipliers compound.</summary>
    Multiply,

    /// <summary>
    /// Replaces the value with <c>Value</c>, ignoring every other modifier. The override with
    /// the highest <c>Priority</c> wins; on a tie, the most recently added one does.
    /// </summary>
    Override,
}
