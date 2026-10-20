using System.Collections.Immutable;
using Ouranos.Foundry.Features.LootTables.Resources;

namespace Ouranos.Foundry.Features.LootTables.Types;

/// <summary>
/// Per-roll state passed to <see cref="LootTableEntry.IsEligible"/> and
/// <see cref="LootTableEntry.Resolve"/>. Tracks recursion depth and the chain of
/// tables currently being rolled so circular sub-table references are detected.
/// <see cref="Deeper"/> copies the callbacks, so events raised in sub-tables reach
/// whoever started the roll.
/// </summary>
public sealed record RollContext
{
    public int Depth { get; init; }

    /// <summary>
    /// Invoked with the (sub-)table that selected an <see cref="EmptyLootEntry"/>.
    /// </summary>
    public Action<LootTableData>? OnEmptyDropSelected { get; init; }

    internal ImmutableHashSet<LootTableData> Ancestors { get; init; } =
        ImmutableHashSet<LootTableData>.Empty;

    public RollContext Deeper()
    {
        return this with { Depth = Depth + 1 };
    }

    internal bool IsRolling(LootTableData table)
    {
        return Ancestors.Contains(table);
    }

    internal RollContext Entering(LootTableData table)
    {
        return this with { Ancestors = Ancestors.Add(table) };
    }
}
