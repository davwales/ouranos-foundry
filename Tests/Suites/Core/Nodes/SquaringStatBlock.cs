using System.Collections.Generic;
using Ouranos.Foundry.Core.Nodes;
using Ouranos.Foundry.Core.Resources;

namespace Ouranos.Foundry.Tests.Core.Nodes;

/// <summary>A custom formula that ignores modifiers and squares the base value.</summary>
public partial class SquaringStatBlock : StatBlock
{
    protected override float Calculate(
        StatDefinition stat,
        float baseValue,
        IReadOnlyList<StatModifier> modifiers
    )
    {
        return baseValue * baseValue;
    }
}
