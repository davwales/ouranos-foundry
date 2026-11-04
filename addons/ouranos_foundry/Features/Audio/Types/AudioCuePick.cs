#nullable enable

using Godot;

namespace Ouranos.Foundry.Features.Audio.Types;

/// <summary>One play of an <c>AudioCue</c>: the chosen stream and its varied volume and pitch.</summary>
public sealed record AudioCuePick(AudioStream Stream, float VolumeDb, float PitchScale);
