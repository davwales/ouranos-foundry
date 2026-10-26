using System;
using Godot;
using Ouranos.Foundry.Features.Persistence.Nodes;
using Ouranos.Foundry.Features.ProceduralGeneration.Nodes;

namespace Ouranos.Foundry.Demos.Persistence;

/// <summary>
/// Saves the procedural world (its blueprint and painted tiles) and the player's position to
/// one slot. Paint, roll a new world, save, quit, and start the demo again: it loads the save
/// on start and shows the same state.
/// </summary>
public partial class PersistenceDemo : Node2D
{
    private const string Slot = "demo";

    [Export]
    public SaveManager? SaveManager { get; private set; }

    [Export]
    public WorldGenerator? Generator { get; private set; }

    [Export]
    public Node2D? Player { get; private set; }

    [Export]
    public Label? StatusLabel { get; private set; }

    [Export]
    public Button? SaveButton { get; private set; }

    [Export]
    public Button? LoadButton { get; private set; }

    [Export]
    public Button? NewWorldButton { get; private set; }

    [Export]
    public Button? DeleteButton { get; private set; }

    [Export]
    public Button? QuitButton { get; private set; }

    public override void _Ready()
    {
        if (SaveManager is null || Generator is null)
        {
            GD.PushError("PersistenceDemo: SaveManager and Generator must be assigned.");
            return;
        }

        // The generator lives in the instanced procgen demo scene, so register it in code
        // instead of adding it to the "saveable" group there.
        SaveManager.Register(Generator);
        SaveManager.Saved += OnSaved;
        SaveManager.Loaded += OnLoaded;
        SaveManager.SaveFailed += OnFailed;
        SaveManager.LoadFailed += OnFailed;

        Connect(SaveButton, SaveGame);
        Connect(LoadButton, LoadGame);
        Connect(NewWorldButton, NewWorld);
        Connect(DeleteButton, DeleteSave);
        Connect(QuitButton, () => GetTree().Quit());

        if (SaveManager.HasSave(Slot))
        {
            LoadGame();
        }
        else
        {
            SetStatus($"No save yet. Seed {Generator.WorldState.Seed}.");
        }
    }

    public override void _ExitTree()
    {
        if (SaveManager is not null)
        {
            SaveManager.Saved -= OnSaved;
            SaveManager.Loaded -= OnLoaded;
            SaveManager.SaveFailed -= OnFailed;
            SaveManager.LoadFailed -= OnFailed;
            if (Generator is not null)
            {
                SaveManager.Unregister(Generator);
            }
        }
    }

    private void SaveGame()
    {
        SaveManager?.Save(Slot, new() { ["seed"] = Generator?.WorldState.Seed ?? 0 });
    }

    private void LoadGame()
    {
        SaveManager?.Load(Slot);
    }

    private void NewWorld()
    {
        if (Generator is null)
        {
            return;
        }

        Generator.ResetBlueprint();
        Generator.WorldState.Seed = (ulong)DateTime.UtcNow.Ticks;
        Generator.GenerateBlueprint();
        SetStatus($"New world, seed {Generator.WorldState.Seed}. Not saved yet.");
    }

    private void DeleteSave()
    {
        var deleted = SaveManager?.DeleteSave(Slot) ?? false;
        SetStatus(deleted ? "Save deleted." : "There is no save to delete.");
    }

    private void OnSaved(string slot)
    {
        SetStatus($"Saved seed {Generator?.WorldState.Seed} to {SaveManager?.GetSlotPath(slot)}.");
    }

    private void OnLoaded(string slot)
    {
        var info = SaveManager?.GetSlotInfo(slot);
        SetStatus(
            $"Loaded the save from {info?.SavedAt.ToLocalTime():g}, seed {Generator?.WorldState.Seed}."
        );
    }

    private void OnFailed(string slot, string error)
    {
        SetStatus($"'{slot}' failed: {error}");
    }

    private void SetStatus(string text)
    {
        if (StatusLabel is not null)
        {
            StatusLabel.Text = text;
        }
    }

    private static void Connect(Button? button, Action action)
    {
        button?.Connect(BaseButton.SignalName.Pressed, Callable.From(action));
    }
}
