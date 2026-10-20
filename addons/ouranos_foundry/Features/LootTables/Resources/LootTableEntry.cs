#nullable enable

using System.Collections.Generic;
using Godot;
using Ouranos.Foundry.Core.Types;
using Ouranos.Foundry.Features.LootTables.Types;

namespace Ouranos.Foundry.Features.LootTables.Resources;

/// <summary>
/// Base class for loot table entries. Subclass it (in your own assembly if you like)
/// and override <see cref="IsEligible"/> for conditional drops and
/// <see cref="Resolve"/> to produce results.
/// </summary>
[GlobalClass]
public abstract partial class LootTableEntry : Resource
{
    [Export]
    public string Id { get; set; } = "";

    [Export]
    public string DisplayName { get; set; } = "";

    [Export]
    public float Weight { get; set; } = 1f;

    /// <summary>
    /// The weight used for selection; negative weights count as zero.
    /// </summary>
    internal float EffectiveWeight => Weight > 0f ? Weight : 0f;

    public virtual bool IsEligible(RollContext context)
    {
        return true;
    }

    public abstract IReadOnlyList<LootResult> Resolve(SeededRng rng, RollContext context);
}
