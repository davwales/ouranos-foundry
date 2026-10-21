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
    public VBoxContainer? ButtonList { get; private set; }

    public override void _Ready()
    {
        if (ButtonList is null)
        {
            GD.PushError("DemoLauncher: ButtonList is not assigned.");
            return;
        }

        foreach (var (name, scenePath) in Demos)
        {
            var button = new Button { Text = name };
            button.Connect(
                BaseButton.SignalName.Pressed,
                Callable.From(() => GetTree().ChangeSceneToFile(scenePath))
            );
            ButtonList.AddChild(button);
        }
    }
}
