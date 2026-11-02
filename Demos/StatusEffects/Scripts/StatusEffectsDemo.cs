using Godot;
using Ouranos.Foundry.Features.Health.Nodes;
using Ouranos.Foundry.Features.StatusEffects.Nodes;
using Ouranos.Foundry.Features.StatusEffects.Resources;
using Ouranos.Foundry.Features.StatusEffects.Types;

namespace Ouranos.Foundry.Demos.StatusEffects;

/// <summary>
/// Adds a button per effect and gives the effects their meaning through the
/// component's signals: rage doubles attacks, poison damages per stack, the potion heals.
/// Regeneration does its own healing (see <see cref="HealOverTimeEffect"/>).
/// </summary>
public partial class StatusEffectsDemo : Control
{
    private const float AttackDamage = 10f;
    private const float PoisonDamagePerStack = 2f;
    private const float PotionHealing = 25f;

    [Export]
    public HealthComponent? Health { get; private set; }

    [Export]
    public StatusEffectComponent? StatusEffects { get; private set; }

    [Export]
    public Godot.Collections.Array<StatusEffectDefinition> Effects { get; private set; } = [];

    [Export]
    public Container? EffectButtons { get; private set; }

    [Export]
    public Container? ActionButtons { get; private set; }

    [Export]
    public Label? HealthLabel { get; private set; }

    [Export]
    public RichTextLabel? EventLog { get; private set; }

    public override void _Ready()
    {
        if (Health is null || StatusEffects is null)
        {
            GD.PushError("StatusEffectsDemo: Health and StatusEffects must be assigned.");
            return;
        }

        foreach (var definition in Effects)
        {
            AddButton(
                EffectButtons,
                $"Apply {definition.DisplayName}",
                () => StatusEffects.ApplyEffect(definition, this)
            );
        }

        AddButton(ActionButtons, "Attack", Attack);
        AddButton(ActionButtons, "Cleanse", StatusEffects.ClearEffects);

        StatusEffects.EffectApplied += OnEffectApplied;
        StatusEffects.EffectRefreshed += OnEffectRefreshed;
        StatusEffects.EffectTicked += OnEffectTicked;
        StatusEffects.EffectRemoved += OnEffectRemoved;
        Health.HealthChanged += OnHealthChanged;
        OnHealthChanged(Health.CurrentHealth, Health.MaxHealth);
    }

    public override void _ExitTree()
    {
        if (StatusEffects is not null)
        {
            StatusEffects.EffectApplied -= OnEffectApplied;
            StatusEffects.EffectRefreshed -= OnEffectRefreshed;
            StatusEffects.EffectTicked -= OnEffectTicked;
            StatusEffects.EffectRemoved -= OnEffectRemoved;
        }

        if (Health is not null)
        {
            Health.HealthChanged -= OnHealthChanged;
        }
    }

    private static void AddButton(Container? container, string text, System.Action onPressed)
    {
        if (container is null)
        {
            return;
        }

        var button = new Button { Text = text };
        button.Connect(BaseButton.SignalName.Pressed, Callable.From(onPressed));
        container.AddChild(button);
    }

    private void Attack()
    {
        var enraged = StatusEffects?.HasEffect("rage") ?? false;
        var damage = enraged ? AttackDamage * 2 : AttackDamage;
        Log($"You attack the training dummy for {damage}{(enraged ? " (enraged!)" : "")}.");
    }

    private void OnEffectApplied(StatusEffectInstance effect)
    {
        Log($"Applied {effect.Definition.DisplayName}.");

        if (effect.Id == "potion")
        {
            Health?.Heal(PotionHealing);
        }
    }

    private void OnEffectRefreshed(StatusEffectInstance effect)
    {
        Log($"Refreshed {effect.Definition.DisplayName}: {StatusEffectBar.FormatLabel(effect)}.");
    }

    private void OnEffectTicked(StatusEffectInstance effect)
    {
        if (effect.Id == "poison")
        {
            Health?.Damage(PoisonDamagePerStack * effect.Stacks);
            Log($"Poison deals {PoisonDamagePerStack * effect.Stacks} (x{effect.Stacks}).");
        }
        else
        {
            Log($"{effect.Definition.DisplayName} ticks.");
        }
    }

    private void OnEffectRemoved(StatusEffectInstance effect, StatusEffectRemovalReason reason)
    {
        if (effect.Definition.DurationType == StatusEffectDurationType.Instant)
        {
            return;
        }

        Log($"{effect.Definition.DisplayName} ended ({reason}).");
    }

    private void OnHealthChanged(float current, float max)
    {
        if (HealthLabel is not null)
        {
            HealthLabel.Text = $"HP {current:F0} / {max:F0}";
        }
    }

    private void Log(string message)
    {
        EventLog?.AppendText($"{message}\n");
    }
}
