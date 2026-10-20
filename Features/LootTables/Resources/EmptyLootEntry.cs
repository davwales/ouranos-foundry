#nullable enable

using System.Collections.Generic;
using Godot;
using Ouranos.Foundry.Core.Types;
using Ouranos.Foundry.Features.LootTables.Types;

namespace Ouranos.Foundry.Features.LootTables.Resources;

[GlobalClass]
public partial class EmptyLootEntry : LootTableEntry
{
    /// <summary>
    /// Produces no results. The owning table reports the selection through
    /// <see cref="RollContext.OnEmptyDropSelected"/>.
    /// </summary>
    public override IReadOnlyList<LootResult> Resolve(SeededRng rng, RollContext context)
    {
        return [];
    }
}
