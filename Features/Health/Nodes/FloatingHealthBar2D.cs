using Ouranos.Foundry.Features.Health.Utils;

namespace Ouranos.Foundry.Features.Health.Nodes;

[GlobalClass]
[Icon("res://Assets/icons/foundry_node.svg")]
public partial class FloatingHealthBar2D : Node2D
{
    [Export]
    public float DisplayDuration { get; private set; } = 2.0f;

    [Export]
    public Vector2 BarSize { get; private set; } = new(64, 8);

    [Export]
    public Color FullColor { get; private set; } = Colors.Red;

    [Export]
    public Color EmptyColor { get; private set; } = new(0.2f, 0.2f, 0.2f, 1f);

    [Export]
    public HealthComponent? Target { get; private set; }

    private Tween? _fadeTween;
    private ColorRect? _background;
    private ColorRect? _fillBar;
    private double _lastDamageTime;
    private readonly HealthMonitorConnection _connection = new();

    public override void _Ready()
    {
        _background = new ColorRect { Color = EmptyColor, Size = BarSize };
        AddChild(_background);

        _fillBar = new ColorRect { Color = FullColor, Size = BarSize };
        AddChild(_fillBar);

        Visible = false;

        if (Target is not null)
        {
            MonitorHealthComponent(Target);
        }
    }

    public void MonitorHealthComponent(HealthComponent? healthNode)
    {
        _connection.Monitor(healthNode, OnHealthChanged, OnHealthDecreased, OnMaxHealthChanged);

        var component = _connection.Component;
        if (component is null)
        {
            Visible = false;
            _fadeTween?.Kill();
            _fadeTween = null;
            return;
        }

        UpdateFill(component.CurrentHealth, component.MaxHealth);
    }

    public override void _Process(double delta)
    {
        if (!Visible || _fadeTween is not null)
        {
            return;
        }

        double currentTime = Time.GetTicksMsec() / 1000.0;
        if (currentTime - _lastDamageTime > DisplayDuration)
        {
            StartFade();
        }
    }

    public override void _ExitTree()
    {
        _fadeTween?.Kill();
        _fadeTween = null;

        _connection.Disconnect();
    }

    private void OnHealthChanged(float current, float max)
    {
        UpdateFill(current, max);
    }

    private void OnHealthDecreased(float amount, float current, Node? source)
    {
        Modulate = Colors.White;
        Visible = true;
        UpdateFill(current, _connection.Component?.MaxHealth ?? 0f);
        _lastDamageTime = Time.GetTicksMsec() / 1000.0;

        _fadeTween?.Kill();
        _fadeTween = null;
    }

    private void OnMaxHealthChanged(float max)
    {
        var component = _connection.Component;
        if (component is null)
        {
            return;
        }

        UpdateFill(component.CurrentHealth, max);
    }

    private void UpdateFill(float current, float max)
    {
        float fill = max > 0f ? current / max : 0f;
        _fillBar!.Size = new Vector2(BarSize.X * fill, BarSize.Y);
    }

    private void StartFade()
    {
        _fadeTween = CreateTween();
        _fadeTween.TweenProperty(this, "modulate:a", 0f, 0.5f);
        _fadeTween.Finished += OnFadeTweenFinished;
    }

    private void OnFadeTweenFinished()
    {
        Visible = false;
        Modulate = Colors.White;
        _fadeTween = null;
    }
}
