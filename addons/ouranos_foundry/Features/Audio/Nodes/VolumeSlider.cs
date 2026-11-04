#nullable enable

using Godot;

namespace Ouranos.Foundry.Features.Audio.Nodes;

/// <summary>
/// A slider for one audio bus's volume, from 0 (silent) to 1. It writes to the bus as it
/// moves and follows changes made elsewhere, such as a loaded save, so a settings menu
/// needs no code. Works with or without an <see cref="AudioManager"/>.
/// </summary>
[GlobalClass]
[Icon("res://addons/ouranos_foundry/Assets/icons/foundry_node.svg")]
public partial class VolumeSlider : HSlider
{
    [Export]
    public StringName Bus { get; private set; } = "Master";

    private int _busIndex = -1;

    public VolumeSlider()
    {
        // Defaults that a scene can override.
        MinValue = 0;
        MaxValue = 1;
        Step = 0.01;
        CustomMinimumSize = new Vector2(160, 0);
    }

    public override void _Ready()
    {
        ValueChanged += OnValueChanged;
    }

    public override void _ExitTree()
    {
        ValueChanged -= OnValueChanged;
    }

    public override void _Process(double delta)
    {
        // Resolved every frame, so buses added after _Ready (e.g. by an AudioManager) and
        // layout changes are picked up.
        _busIndex = AudioServer.GetBusIndex(Bus);
        Editable = _busIndex >= 0;
        if (_busIndex < 0)
        {
            return;
        }

        var volume = AudioServer.GetBusVolumeLinear(_busIndex);
        if (Mathf.Abs(volume - Value) > Step / 2)
        {
            SetValueNoSignal(volume);
        }
    }

    private void OnValueChanged(double value)
    {
        if (_busIndex >= 0)
        {
            AudioServer.SetBusVolumeLinear(_busIndex, (float)value);
        }
    }
}
