#nullable enable

using Godot;
using Ouranos.Foundry.Features.Health.Utils;

namespace Ouranos.Foundry.Features.Health.Nodes;

[GlobalClass]
[Icon("res://addons/ouranos_foundry/Assets/icons/foundry_node.svg")]
public partial class FloatingHealthBar3D : Node3D
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
    public bool Billboard { get; private set; } = true;

    [Export]
    public HealthComponent? Target { get; private set; }

    private Tween? _fadeTween;
    private SubViewport? _viewport;
    private ColorRect? _background;
    private ColorRect? _fillBar;
    private Sprite3D? _sprite;
    private double _lastDamageTime;
    private readonly HealthMonitorConnection _connection = new();

    public override void _Ready()
    {
        _viewport = new SubViewport
        {
            Size = new Vector2I((int)BarSize.X, (int)BarSize.Y),
            TransparentBg = true,
        };
        AddChild(_viewport);

        _background = new ColorRect { Color = EmptyColor, Size = BarSize };
        _viewport.AddChild(_background);

        _fillBar = new ColorRect { Color = FullColor, Size = BarSize };
        _viewport.AddChild(_fillBar);

        _sprite = new Sprite3D
        {
            Billboard = Billboard
                ? BaseMaterial3D.BillboardModeEnum.Enabled
                : BaseMaterial3D.BillboardModeEnum.Disabled,
        };

        AddChild(_sprite);

        Visible = false;

        CallDeferred(nameof(AssignViewportTexture));

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

    private void AssignViewportTexture()
    {
        if (_sprite is not null && _viewport is not null)
        {
            _sprite.Texture = _viewport.GetTexture();
        }
    }

    private void OnHealthChanged(float current, float max)
    {
        UpdateFill(current, max);
    }

    private void OnHealthDecreased(float amount, float current, Node? source)
    {
        if (_sprite is not null)
        {
            _sprite.Modulate = Colors.White;
        }

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
        if (_sprite is null)
        {
            return;
        }

        _fadeTween = CreateTween();
        _fadeTween.TweenProperty(_sprite, "modulate:a", 0f, 0.5f);
        _fadeTween.Finished += OnFadeTweenFinished;
    }

    private void OnFadeTweenFinished()
    {
        Visible = false;
        if (_sprite is not null)
        {
            _sprite.Modulate = Colors.White;
        }

        _fadeTween = null;
    }
}
