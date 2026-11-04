#nullable enable

namespace Ouranos.Foundry.Features.Audio.Types;

/// <summary>Where a pooled sound plays: everywhere, or at a position in a 2D or 3D world.</summary>
public enum SoundPositioning
{
    /// <summary>An <c>AudioStreamPlayer</c>: no position, e.g. UI sounds.</summary>
    None,

    /// <summary>An <c>AudioStreamPlayer2D</c>.</summary>
    Positional2D,

    /// <summary>An <c>AudioStreamPlayer3D</c>.</summary>
    Positional3D,
}
