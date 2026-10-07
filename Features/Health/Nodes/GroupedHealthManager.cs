using Ouranos.Foundry.Features.Health.Utils;

namespace Ouranos.Foundry.Features.Health.Nodes;

[GlobalClass]
[Icon("res://Assets/icons/foundry_node.svg")]
public partial class GroupedHealthManager : Control
{
    [Export]
    public float BarWidth { get; private set; } = 200f;

    [Export]
    public float BarHeight { get; private set; } = 16f;

    [Export]
    public Color BarFullColor { get; private set; } = Colors.Red;

    [Export]
    public Color BarEmptyColor { get; private set; } = new(0.15f, 0.15f, 0.15f, 1f);

    [Export]
    public int FontSize { get; private set; } = 14;

    [Export]
    public string LabelFormat { get; private set; } = "{Name}";

    [Export]
    public HealthComponent? Target { get; private set; }

    private readonly Dictionary<HealthComponent, BarEntry> _entries = [];
    private VBoxContainer? _container;

    public override void _Ready()
    {
        _container = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.Fill,
            SizeFlagsVertical = SizeFlags.Fill,
        };
        AddChild(_container);

        CustomMinimumSize = new Vector2(BarWidth, 0);

        if (Target is not null)
        {
            MonitorHealthComponent(Target);
        }
    }

    public void MonitorHealthComponent(HealthComponent? healthNode)
    {
        if (healthNode is null)
        {
            return;
        }

        RegisterComponent(healthNode);
    }

    public void RegisterComponent(HealthComponent component, string? label = null)
    {
        if (_entries.ContainsKey(component))
        {
            return;
        }

        string entityName = label ?? component.GetParent()?.Name ?? "Entity";
        var (row, fillBar, nameLabel) = CreateBarRow(entityName);
        _container?.AddChild(row);

        var connection = new HealthMonitorConnection();
        connection.Monitor(
            component,
            (current, max) => OnComponentHealthChanged(component, current, max),
            null,
            max => OnComponentMaxHealthChanged(component, max)
        );

        Action treeHandler = () => UnregisterComponent(component);
        var treeCallable = Callable.From(treeHandler);
        component.Connect(Node.SignalName.TreeExiting, treeCallable);

        _entries[component] = new BarEntry(
            row,
            fillBar,
            nameLabel,
            entityName,
            connection,
            treeCallable
        );

        UpdateLabel(component, entityName);
        UpdateFill(component, component.CurrentHealth, component.MaxHealth);
    }

    private (HBoxContainer row, ColorRect fillBar, Label nameLabel) CreateBarRow(string entityName)
    {
        var row = new HBoxContainer();

        var nameLabel = new Label
        {
            CustomMinimumSize = new Vector2(BarWidth * 0.35f, 0),
            ClipText = true,
        };
        nameLabel.AddThemeFontSizeOverride("font_size", FontSize);
        row.AddChild(nameLabel);

        var background = new ColorRect
        {
            Color = BarEmptyColor,
            CustomMinimumSize = new Vector2(BarWidth * 0.65f, BarHeight),
        };

        var fillBar = new ColorRect
        {
            Color = BarFullColor,
            Size = new Vector2(BarWidth * 0.65f, BarHeight),
        };
        background.AddChild(fillBar);
        row.AddChild(background);

        return (row, fillBar, nameLabel);
    }

    public void UnregisterComponent(HealthComponent component)
    {
        if (!_entries.TryGetValue(component, out var entry))
        {
            return;
        }

        entry.Connection.Disconnect();
        component.Disconnect(Node.SignalName.TreeExiting, entry.TreeExitingCallable);
        // Detach now so the row disappears this frame; QueueFree alone defers removal.
        entry.Row.GetParent()?.RemoveChild(entry.Row);
        entry.Row.QueueFree();
        _entries.Remove(component);
    }

    public override void _ExitTree()
    {
        var components = _entries.Keys.ToList();
        foreach (var component in components)
        {
            UnregisterComponent(component);
        }
    }

    private void OnComponentHealthChanged(HealthComponent component, float current, float max)
    {
        UpdateFill(component, current, max);
        UpdateLabel(component, GetEntityName(component));
    }

    private void OnComponentMaxHealthChanged(HealthComponent component, float max)
    {
        UpdateFill(component, component.CurrentHealth, max);
        UpdateLabel(component, GetEntityName(component));
    }

    private string GetEntityName(HealthComponent component)
    {
        if (_entries.TryGetValue(component, out var entry))
        {
            return entry.EntityName;
        }

        return component.GetParent()?.Name ?? "Entity";
    }

    private void UpdateFill(HealthComponent component, float current, float max)
    {
        if (!_entries.TryGetValue(component, out var entry))
        {
            return;
        }

        float fraction = max > 0f ? current / max : 0f;
        entry.FillBar.Size = new Vector2(BarWidth * 0.65f * fraction, BarHeight);
    }

    private void UpdateLabel(HealthComponent component, string entityName)
    {
        if (!_entries.TryGetValue(component, out var entry))
        {
            return;
        }

        entry.NameLabel.Text = LabelFormat
            .Replace("{Name}", entityName)
            .Replace("{Current}", component.CurrentHealth.ToString("F0"))
            .Replace("{Max}", component.MaxHealth.ToString("F0"))
            .Replace("{Fraction}", $"{component.HealthFraction * 100f:F0}%");
    }

    private sealed record BarEntry(
        Control Row,
        ColorRect FillBar,
        Label NameLabel,
        string EntityName,
        HealthMonitorConnection Connection,
        Callable TreeExitingCallable
    );
}
