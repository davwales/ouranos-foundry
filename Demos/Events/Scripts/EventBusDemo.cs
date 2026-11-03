using Godot;
using Ouranos.Foundry.Core.Nodes;
using Ouranos.Foundry.Core.Types;

namespace Ouranos.Foundry.Demos.Events;

/// <summary>
/// Publishes game events from buttons. The listeners — the event log here, the
/// <see cref="AchievementTracker"/> and a GDScript gold counter behind an
/// <see cref="EventBusBridge"/> — hold no references to the publisher or to each other.
/// </summary>
public partial class EventBusDemo : Control
{
    [Export]
    public Container? ActionButtons { get; private set; }

    [Export]
    public Node? Achievements { get; private set; }

    [Export]
    public EventBusBridge? Bridge { get; private set; }

    [Export]
    public Label? SubscriberLabel { get; private set; }

    [Export]
    public RichTextLabel? EventLog { get; private set; }

    public override void _EnterTree()
    {
        EventBus.Subscribe<EnemyDefeated>(
            this,
            evt => Log($"Defeated a {evt.Enemy} (+{evt.Experience} XP).")
        );
        EventBus.Subscribe<NamedEvent>(
            this,
            evt => Log($"Named event '{evt.Name}' ({evt.Payload}).")
        );
    }

    public override void _Ready()
    {
        AddButton("Defeat a slime", () => EventBus.Publish(new EnemyDefeated("slime", 5)));
        AddButton("Open a chest", () => EventBus.Publish(new NamedEvent("chest_opened", 10)));
        AddButton("Ring the bell (via bridge)", () => Bridge?.Publish("bell_rung"));
        AddButton("Remove achievements", RemoveAchievements);
        UpdateSubscriberLabel();
    }

    private void RemoveAchievements()
    {
        if (Achievements is null || !IsInstanceValid(Achievements))
        {
            return;
        }

        Achievements.Connect(
            Node.SignalName.TreeExited,
            Callable.From(UpdateSubscriberLabel),
            (uint)ConnectFlags.OneShot
        );
        Achievements.QueueFree();
        Log("Removed the achievement tracker. It unsubscribed when it left the tree.");
    }

    private void AddButton(string text, System.Action onPressed)
    {
        var button = new Button { Text = text };
        button.Connect(BaseButton.SignalName.Pressed, Callable.From(onPressed));
        ActionButtons?.AddChild(button);
    }

    private void UpdateSubscriberLabel()
    {
        if (SubscriberLabel is not null)
        {
            SubscriberLabel.Text =
                $"Subscribers: EnemyDefeated {EventBus.SubscriberCount<EnemyDefeated>()}, "
                + $"NamedEvent {EventBus.SubscriberCount<NamedEvent>()}";
        }
    }

    private void Log(string message)
    {
        EventLog?.AppendText($"{message}\n");
    }
}
