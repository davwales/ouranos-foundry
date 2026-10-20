#nullable enable

using Godot;
using Ouranos.Foundry.Features.Health.Utils;

namespace Ouranos.Foundry.Features.Health.Nodes;

[GlobalClass]
[Icon("res://addons/ouranos_foundry/Assets/icons/foundry_node.svg")]
public partial class PlayerHealthBar : Control
{
    [Export]
    public float CatchUpDuration { get; private set; } = 0.5f;

    [Export]
    public Color MainBarColor { get; private set; } = Colors.Red;

    [Export]
    public Color LagBarColor { get; private set; } = new(0.6f, 0.1f, 0.1f, 1f);

    [Export]
    public Color BackgroundColor { get; private set; } = new(0.15f, 0.15f, 0.15f, 1f);

    [Export]
    public Vector2 BarSize { get; private set; } = new(300, 24);

    [Export]
    public HealthComponent? Target { get; private set; }

    private float _mainFill;
    private float _lagFill;
    private ColorRect? _background;
    private ColorRect? _lagBar;
    private ColorRect? _mainBar;
    private readonly HealthMonitorConnection _connection = new();

    public override void _Ready()
    {
        _background = new ColorRect { Color = BackgroundColor, Size = BarSize };
        AddChild(_background);

        _lagBar = new ColorRect { Color = LagBarColor, Size = BarSize };
        AddChild(_lagBar);

        _mainBar = new ColorRect { Color = MainBarColor, Size = BarSize };
        AddChild(_mainBar);

        CustomMinimumSize = BarSize;

        if (Target is not null)
        {
            MonitorHealthComponent(Target);
        }
    }

    public void MonitorHealthComponent(HealthComponent? healthNode)
    {
        _connection.Monitor(healthNode, OnHealthChanged, null, OnMaxHealthChanged);

        var component = _connection.Component;
        if (component is null)
        {
            _mainFill = 0f;
            _lagFill = 0f;
            UpdateBarSizes();
            return;
        }

        float fill = component.MaxHealth > 0f ? component.CurrentHealth / component.MaxHealth : 0f;
        _mainFill = fill;
        _lagFill = fill;
        UpdateBarSizes();
    }

    public override void _Process(double delta)
    {
        if (CatchUpDuration <= 0f)
        {
            return;
        }

        if (_lagFill > _mainFill + 0.001f)
        {
            _lagFill = Mathf.MoveToward(_lagFill, _mainFill, (float)delta / CatchUpDuration);
            _lagBar!.Size = new Vector2(_lagFill * BarSize.X, BarSize.Y);
        }
    }

    public override void _ExitTree()
    {
        _connection.Disconnect();
    }

    private void OnHealthChanged(float current, float max)
    {
        float newFill = max > 0f ? current / max : 0f;

        if (newFill < _mainFill)
        {
            _mainFill = newFill;
            if (_mainBar is not null)
            {
                _mainBar.Size = new Vector2(_mainFill * BarSize.X, BarSize.Y);
            }
        }
        else
        {
            _mainFill = newFill;
            _lagFill = newFill;
            UpdateBarSizes();
        }
    }

    private void OnMaxHealthChanged(float max)
    {
        var component = _connection.Component;
        if (component is null)
        {
            return;
        }

        float fill = max > 0f ? component.CurrentHealth / max : 0f;
        _mainFill = fill;
        _lagFill = fill;
        UpdateBarSizes();
    }

    private void UpdateBarSizes()
    {
        _mainBar?.Size = new Vector2(_mainFill * BarSize.X, BarSize.Y);
        _lagBar?.Size = new Vector2(_lagFill * BarSize.X, BarSize.Y);
    }
}
