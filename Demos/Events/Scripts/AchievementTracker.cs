using Godot;
using Ouranos.Foundry.Core.Types;

namespace Ouranos.Foundry.Demos.Events;

/// <summary>
/// Unlocks an achievement after a number of kills. It knows nothing about who defeats
/// enemies: it only listens on the bus, and stops listening when it leaves the tree.
/// </summary>
public partial class AchievementTracker : Label
{
    [Export]
    public int KillsToUnlock { get; private set; } = 3;

    private int _kills;

    public override void _EnterTree()
    {
        EventBus.Subscribe<EnemyDefeated>(this, OnEnemyDefeated);
        UpdateText();
    }

    private void OnEnemyDefeated(EnemyDefeated evt)
    {
        _kills++;
        UpdateText();
    }

    private void UpdateText()
    {
        Text =
            _kills >= KillsToUnlock
                ? $"🏆 Achievement unlocked: Slayer ({_kills} kills)"
                : $"Achievement: Slayer ({_kills} / {KillsToUnlock} kills)";
    }
}
