#nullable enable

using Godot;

namespace Ouranos.Foundry.Features.Health.Types;

public sealed record HeartVisualDefinition(
    Color? FullColor = null,
    Color? HalfColor = null,
    Color? EmptyColor = null,
    Texture2D? FullTexture = null,
    Texture2D? HalfTexture = null,
    Texture2D? EmptyTexture = null
);
