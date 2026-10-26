#nullable enable

using System;
using Godot;

namespace Ouranos.Foundry.Features.Persistence.Types;

/// <summary>
/// A save slot's header: the game data version it was written with, when (UTC), and the
/// metadata passed to <c>SaveManager.Save</c>.
/// </summary>
public sealed record SaveSlotInfo(
    string Slot,
    int Version,
    DateTime SavedAt,
    Godot.Collections.Dictionary<string, Variant> Metadata
);
