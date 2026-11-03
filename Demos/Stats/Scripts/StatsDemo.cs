using System.Collections.Generic;
using System.Linq;
using Godot;
using Ouranos.Foundry.Core.Nodes;
using Ouranos.Foundry.Core.Resources;
using Ouranos.Foundry.Core.Types;
using Ouranos.Foundry.Features.Health.Nodes;

namespace Ouranos.Foundry.Demos.Stats;

/// <summary>
/// A stat panel with a toggle per <see cref="ModifierSet"/>. Max health comes from the
/// stat block through <see cref="HealthComponent.MaxHealthProvider"/>.
/// </summary>
public partial class StatsDemo : Control
{
    private const string MaxHealthStat = "max_health";

    [Export]
    public StatBlock? Stats { get; private set; }

    [Export]
    public HealthComponent? Health { get; private set; }

    [Export]
    public Godot.Collections.Array<ModifierSet> ModifierSets { get; private set; } = [];

    [Export]
    public GridContainer? StatGrid { get; private set; }

    [Export]
    public Container? ModifierButtons { get; private set; }

    [Export]
    public Container? ActionButtons { get; private set; }

    [Export]
    public Label? HealthLabel { get; private set; }

    [Export]
    public RichTextLabel? EventLog { get; private set; }

    private readonly Dictionary<StringName, Label[]> _rows = [];

    public override void _Ready()
    {
        if (Stats is null || Health is null)
        {
            GD.PushError("StatsDemo: Stats and Health must be assigned.");
            return;
        }

        foreach (var set in ModifierSets)
        {
            var toggle = new CheckButton { Text = set.DisplayName };
            toggle.Connect(
                BaseButton.SignalName.Toggled,
                Callable.From<bool>(pressed => Toggle(set, pressed))
            );
            ModifierButtons?.AddChild(toggle);
        }

        AddButton("Level up", LevelUp);
        AddButton("Take 25 damage", () => Health.Damage(25f));
        AddButton("Heal fully", () => Health.Heal(Health.MaxHealth));

        Health.MaxHealthProvider = () => Stats.GetValue(MaxHealthStat);
        Health.RefreshMaxHealth();

        Stats.StatChanged += OnStatChanged;
        Health.HealthChanged += OnHealthChanged;
        OnHealthChanged(Health.CurrentHealth, Health.MaxHealth);
        BuildPanel();
    }

    public override void _ExitTree()
    {
        if (Stats is not null)
        {
            Stats.StatChanged -= OnStatChanged;
        }

        if (Health is not null)
        {
            Health.HealthChanged -= OnHealthChanged;
            Health.MaxHealthProvider = null;
        }
    }

    private void Toggle(ModifierSet set, bool active)
    {
        if (!active)
        {
            Log($"Removed {set.DisplayName}.");
            Stats?.RemoveModifiersFrom(set);
            return;
        }

        Log($"Applied {set.DisplayName}.");
        foreach (var modifier in set.Modifiers)
        {
            Stats?.AddModifier(modifier, set);
        }
    }

    private void LevelUp()
    {
        if (Stats is null)
        {
            return;
        }

        Log("Level up: +2 strength, +10 max health.");
        Stats.SetBaseValue("strength", Stats.GetBaseValue("strength") + 2f);
        Stats.SetBaseValue(MaxHealthStat, Stats.GetBaseValue(MaxHealthStat) + 10f);

        // StatChanged reports final values; refresh the base column even if they didn't move.
        UpdateRow("strength");
        UpdateRow(MaxHealthStat);
    }

    private void OnStatChanged(StringName statId, float oldValue, float newValue)
    {
        Log($"  {statId}: {oldValue:0.##} → {newValue:0.##}");
        if (statId == MaxHealthStat)
        {
            Health?.RefreshMaxHealth();
        }

        UpdateRow(statId);
    }

    private void OnHealthChanged(float current, float max)
    {
        if (HealthLabel is not null)
        {
            HealthLabel.Text = $"HP {current:0} / {max:0}";
        }
    }

    private void BuildPanel()
    {
        if (Stats is null || StatGrid is null)
        {
            return;
        }

        foreach (var header in new[] { "Stat", "Base", "Final", "Modifiers" })
        {
            StatGrid.AddChild(new Label { Text = header, Modulate = new Color(1, 1, 1, 0.6f) });
        }

        foreach (var stat in Stats.Stats)
        {
            var row = new[] { new Label(), new Label(), new Label(), new Label() };
            row[0].Text = stat.DisplayName;
            foreach (var label in row)
            {
                StatGrid.AddChild(label);
            }

            _rows[stat.Id] = row;
            UpdateRow(stat.Id);
        }
    }

    private void UpdateRow(StringName statId)
    {
        if (Stats is null || !_rows.TryGetValue(statId, out var row))
        {
            return;
        }

        row[1].Text = $"{Stats.GetBaseValue(statId):0.##}";
        row[2].Text = $"{Stats.GetValue(statId):0.##}";
        row[3].Text = string.Join("  ", Stats.GetModifiers(statId).Select(Describe));
    }

    private static string Describe(StatModifier modifier)
    {
        return modifier.Operation switch
        {
            StatModifierOperation.Add => $"{modifier.Value:+0.##;-0.##}",
            StatModifierOperation.PercentAdd => $"{modifier.Value * 100:+0.##;-0.##}%",
            StatModifierOperation.Multiply => $"×{modifier.Value:0.##}",
            _ => $"={modifier.Value:0.##}",
        };
    }

    private void AddButton(string text, System.Action onPressed)
    {
        var button = new Button { Text = text };
        button.Connect(BaseButton.SignalName.Pressed, Callable.From(onPressed));
        ActionButtons?.AddChild(button);
    }

    private void Log(string message)
    {
        EventLog?.AppendText($"{message}\n");
    }
}
