namespace Ouranos.Foundry.Features.LootTables;

/// <summary>
/// Per-roll state threaded through nested tables. <see cref="Deeper"/> copies the
/// callbacks, so events raised in sub-tables reach whoever started the roll.
/// </summary>
internal record RollContext(int Depth = 0, Action<LootTableData>? OnEmptyDropSelected = null)
{
    public RollContext Deeper() => this with { Depth = Depth + 1 };
}
