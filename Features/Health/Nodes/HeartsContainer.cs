using Ouranos.Foundry.Features.Health.Types;
using Ouranos.Foundry.Features.Health.Utils;

namespace Ouranos.Foundry.Features.Health.Nodes;

[GlobalClass]
[Icon("res://Assets/icons/foundry_node.svg")]
public partial class HeartsContainer : HBoxContainer
{
    private static readonly Color _defaultFullColor = new(0.9f, 0.1f, 0.1f, 1f);
    private static readonly Color _defaultHalfColor = new(0.45f, 0.05f, 0.05f, 1f);
    private static readonly Color _defaultEmptyColor = new(0.15f, 0.15f, 0.15f, 1f);
    private static readonly Vector2 _heartSize = new(24, 24);

    [Export]
    public float HealthPointsPerHeart { get; private set; } = 20f;

    [Export]
    public HealthComponent? Target { get; private set; }

    public List<HeartVisualDefinition> HeartVisuals { get; set; } = [];

    private readonly HealthMonitorConnection _connection = new();

    public override void _Ready()
    {
        if (Target is not null)
        {
            MonitorHealthComponent(Target);
        }

        CustomMinimumSize = new Vector2(0, 24);
    }

    public void MonitorHealthComponent(HealthComponent? healthNode)
    {
        _connection.Monitor(healthNode, OnHealthChanged, null, OnMaxHealthChanged);

        var component = _connection.Component;
        if (component is null)
        {
            ClearHearts();
            return;
        }

        RebuildHearts(component.CurrentHealth, component.MaxHealth);
    }

    public override void _ExitTree()
    {
        _connection.Disconnect();
    }

    private void OnHealthChanged(float current, float max)
    {
        RebuildHearts(current, max);
    }

    private void OnMaxHealthChanged(float max)
    {
        var component = _connection.Component;
        if (component is null)
        {
            return;
        }

        RebuildHearts(component.CurrentHealth, max);
    }

    private void ClearHearts()
    {
        foreach (var child in GetChildren())
        {
            RemoveChild(child);
            child.Free();
        }
    }

    private void RebuildHearts(float current, float max)
    {
        ClearHearts();

        if (HealthPointsPerHeart <= 0f)
        {
            return;
        }

        int totalHearts = Mathf.CeilToInt(max / HealthPointsPerHeart);
        float heartFill = current / HealthPointsPerHeart;

        for (int i = 0; i < totalHearts; i++)
        {
            HeartVisualDefinition? definition = i < HeartVisuals.Count ? HeartVisuals[i] : null;
            var state = GetHeartState(heartFill, i);
            var heart = CreateHeartControl(definition, state);
            heart.CustomMinimumSize = _heartSize;
            AddChild(heart);
        }
    }

    private static HeartState GetHeartState(float heartFill, int index)
    {
        if (heartFill >= index + 1)
        {
            return HeartState.Full;
        }

        if (heartFill >= index + 0.5f)
        {
            return HeartState.Half;
        }

        return HeartState.Empty;
    }

    private static Control CreateHeartControl(HeartVisualDefinition? definition, HeartState state)
    {
        Texture2D? texture = state switch
        {
            HeartState.Full => definition?.FullTexture,
            HeartState.Half => definition?.HalfTexture,
            HeartState.Empty => definition?.EmptyTexture,
            _ => null,
        };

        if (texture is not null)
        {
            return new TextureRect
            {
                Texture = texture,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            };
        }

        Color color = state switch
        {
            HeartState.Full => definition?.FullColor ?? _defaultFullColor,
            HeartState.Half => definition?.HalfColor ?? _defaultHalfColor,
            HeartState.Empty => definition?.EmptyColor ?? _defaultEmptyColor,
            _ => _defaultEmptyColor,
        };

        return new ColorRect { Color = color };
    }

    private enum HeartState
    {
        Full,
        Half,
        Empty,
    }
}
