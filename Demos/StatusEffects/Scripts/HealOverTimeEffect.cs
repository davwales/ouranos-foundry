using Godot;
using Ouranos.Foundry.Features.Health.Nodes;
using Ouranos.Foundry.Features.StatusEffects.Nodes;
using Ouranos.Foundry.Features.StatusEffects.Resources;

namespace Ouranos.Foundry.Demos.StatusEffects;

/// <summary>
/// An effect that does its own work through a hook: each tick heals the
/// <see cref="HealthComponent"/> next to the target's status effect component.
/// </summary>
public partial class HealOverTimeEffect : StatusEffectDefinition
{
    [Export]
    public float HealPerTick { get; set; } = 3f;

    public override void OnTicked(StatusEffectComponent target, StatusEffectInstance effect)
    {
        foreach (var sibling in target.GetParent().GetChildren())
        {
            if (sibling is HealthComponent health)
            {
                health.Heal(HealPerTick * effect.Stacks);
                return;
            }
        }
    }
}
