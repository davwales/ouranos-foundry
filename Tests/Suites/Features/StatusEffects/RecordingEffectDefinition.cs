using System.Collections.Generic;
using Ouranos.Foundry.Features.StatusEffects.Nodes;
using Ouranos.Foundry.Features.StatusEffects.Resources;
using Ouranos.Foundry.Features.StatusEffects.Types;

namespace Ouranos.Foundry.Tests.Features.StatusEffects;

/// <summary>
/// Appends every hook call to a shared log.
/// </summary>
public partial class RecordingEffectDefinition : StatusEffectDefinition
{
    public List<string> Log { get; set; } = [];

    public override void OnApplied(StatusEffectComponent target, StatusEffectInstance effect)
    {
        Log.Add("OnApplied");
    }

    public override void OnRefreshed(StatusEffectComponent target, StatusEffectInstance effect)
    {
        Log.Add("OnRefreshed");
    }

    public override void OnTicked(StatusEffectComponent target, StatusEffectInstance effect)
    {
        Log.Add("OnTicked");
    }

    public override void OnRemoved(
        StatusEffectComponent target,
        StatusEffectInstance effect,
        StatusEffectRemovalReason reason
    )
    {
        Log.Add($"OnRemoved({reason})");
    }
}
