using Godot;

namespace Ouranos.Foundry.Demos.Launcher;

public partial class DemoLauncher : Control
{
    private static readonly (string Name, string ScenePath)[] Demos =
    [
        ("Health", "res://Demos/Health/Scenes/HealthDemo.tscn"),
        ("Loot Tables", "res://Demos/LootTables/Scenes/LootTableDemo.tscn"),
        (
            "Procedural Generation",
            "res://Demos/ProceduralGeneration/Scenes/ProceduralGeneration.tscn"
        ),
    ];

    [Export]
    private VBoxContainer? _buttonList;

    public override void _Ready()
    {
        if (_buttonList is null)
        {
            GD.PushError("DemoLauncher: _buttonList is not assigned.");
            return;
        }

        foreach (var (name, scenePath) in Demos)
        {
            var button = new Button { Text = name };
            button.Connect(
                BaseButton.SignalName.Pressed,
                Callable.From(() => GetTree().ChangeSceneToFile(scenePath))
            );
            _buttonList.AddChild(button);
        }
    }
}
