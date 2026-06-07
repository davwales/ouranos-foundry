namespace Ouranos.Foundry.Features.LootTables;

internal record RollContext(int Depth = 0)
{
    public RollContext Deeper() => this with { Depth = Depth + 1 };
}
