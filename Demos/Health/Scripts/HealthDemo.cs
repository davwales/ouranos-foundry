using Ouranos.Foundry.Features.Health.Nodes;

namespace Ouranos.Foundry.Demos.Health;

public partial class HealthDemo : Control
{
    [Export]
    private PlayerHealthBar? _playerHealthBar;

    [Export]
    private HeartsContainer? _heartsContainer;

    [Export]
    private FloatingHealthBar2D? _floatingHealthBar;

    [Export]
    private GroupedHealthManager? _groupedHealthManager;

    [Export]
    private Label? _playerInfoLabel;

    [Export]
    private RichTextLabel? _eventLog;

    [Export]
    private HealthComponent? _playerHealth;

    [Export]
    private HealthComponent? _enemy1Health;

    [Export]
    private HealthComponent? _enemy2Health;

    [Export]
    private Button? _damage10Button;

    [Export]
    private Button? _damage25Button;

    [Export]
    private Button? _healButton;

    [Export]
    private Button? _killButton;

    [Export]
    private Button? _reviveButton;

    [Export]
    private Button? _toggleInvulnButton;

    [Export]
    private Button? _buffButton;

    [Export]
    private Button? _setMaxHealthButton;

    [Export]
    private Button? _damageEnemy1Button;

    [Export]
    private Button? _damageEnemy2Button;

    private readonly float[] _maxHealthCycle = [100f, 150f, 75f];
    private int _cycleIndex;

    public override void _Ready()
    {
        WirePlayerButtons();
        WireEnemyButtons();
        WireUIToHealthComponents();
        SubscribeToHealthSignals();
        UpdatePlayerInfo();
    }

    private void WirePlayerButtons()
    {
        if (_damage10Button is { })
        {
            _damage10Button.Pressed += () =>
            {
                _playerHealth?.Damage(10f);
                Log("[Player] Dealt 10 damage");
                UpdatePlayerInfo();
            };
        }

        if (_damage25Button is { })
        {
            _damage25Button.Pressed += () =>
            {
                _playerHealth?.Damage(25f);
                Log("[Player] Dealt 25 damage");
                UpdatePlayerInfo();
            };
        }

        if (_healButton is { })
        {
            _healButton.Pressed += () =>
            {
                _playerHealth?.Heal(15f);
                Log("[Player] Healed 15 HP");
                UpdatePlayerInfo();
            };
        }

        if (_killButton is { })
        {
            _killButton.Pressed += () =>
            {
                _playerHealth?.Damage(999f);
                Log("[Player] KILLED!");
                UpdatePlayerInfo();
            };
        }

        if (_reviveButton is { })
        {
            _reviveButton.Pressed += () =>
            {
                if (_playerHealth is null)
                {
                    return;
                }

                var healed = _playerHealth.Heal(50f);

                if (healed > 0f)
                {
                    Log("[Player] Revived! +50 HP");
                }
                else if (_playerHealth.IsDead)
                {
                    Log(
                        "[Player] Revive attempted, but AllowRevival is disabled in the inspector."
                    );
                }
                else
                {
                    Log("[Player] Heal attempted, but already at full health.");
                }

                UpdatePlayerInfo();
            };
        }

        if (_toggleInvulnButton is { })
        {
            _toggleInvulnButton.Pressed += () =>
            {
                if (_playerHealth is null)
                {
                    return;
                }

                _playerHealth.SetInvulnerable(!_playerHealth.IsInvulnerable);
                _toggleInvulnButton.Text = _playerHealth.IsInvulnerable
                    ? "Disable Invuln"
                    : "Toggle Invuln";
                Log(
                    _playerHealth.IsInvulnerable
                        ? "[Player] Invulnerability ON"
                        : "[Player] Invulnerability OFF"
                );
            };
        }

        if (_buffButton is { })
        {
            _buffButton.Pressed += () =>
            {
                if (_playerHealth is null)
                {
                    return;
                }

                if (_playerHealth.MaxHealthProvider is null)
                {
                    _playerHealth.MaxHealthProvider = () => 150f;
                    _playerHealth.RefreshMaxHealth();
                    _buffButton.Text = "Deactivate Berserk";
                    Log("[Player] Berserk activated — MaxHealthProvider = 150");
                }
                else
                {
                    _playerHealth.MaxHealthProvider = null;
                    _playerHealth.RefreshMaxHealth();
                    _buffButton.Text = "Activate Berserk";
                    Log("[Player] Berserk deactivated — MaxHealthProvider cleared");
                }

                UpdatePlayerInfo();
            };
        }

        if (_setMaxHealthButton is { })
        {
            _setMaxHealthButton.Pressed += () =>
            {
                if (_playerHealth is null)
                {
                    return;
                }

                _cycleIndex = (_cycleIndex + 1) % _maxHealthCycle.Length;
                var newMax = _maxHealthCycle[_cycleIndex];
                _playerHealth.SetBaseMaxHealth(newMax);
                Log(
                    $"[Player] SetBaseMaxHealth({newMax}) — max is now {_playerHealth.MaxHealth:F0}"
                );
                UpdatePlayerInfo();
            };
        }
    }

    private void WireEnemyButtons()
    {
        if (_damageEnemy1Button is { })
        {
            _damageEnemy1Button.Pressed += () =>
            {
                _enemy1Health?.Damage(15f);
                Log("[Goblin] Dealt 15 damage");
            };
        }

        if (_damageEnemy2Button is { })
        {
            _damageEnemy2Button.Pressed += () =>
            {
                _enemy2Health?.Damage(20f);
                Log("[Orc] Dealt 20 damage");
            };
        }
    }

    private void WireUIToHealthComponents()
    {
        _playerHealthBar?.MonitorHealthComponent(_playerHealth);
        _heartsContainer?.MonitorHealthComponent(_playerHealth);
        _floatingHealthBar?.MonitorHealthComponent(_playerHealth);

        if (_enemy1Health is { } enemy1)
        {
            _groupedHealthManager?.RegisterComponent(enemy1, "Goblin Scout");
        }

        if (_enemy2Health is { } enemy2)
        {
            _groupedHealthManager?.RegisterComponent(enemy2, "Orc Warrior");
        }
    }

    private void SubscribeToHealthSignals()
    {
        if (_playerHealth is { })
        {
            _playerHealth.HealthChanged += (current, max) =>
                Log($"[Player] HealthChanged: {current}/{max}");
            _playerHealth.MaxHealthChanged += max => Log($"[Player] MaxHealthChanged: {max}");
            _playerHealth.HealthDecreased += (amount, current, source) =>
                Log($"[Player] HealthDecreased: -{amount} → {current}");
            _playerHealth.HealthIncreased += (amount, current) =>
                Log($"[Player] HealthIncreased: +{amount} → {current}");
            _playerHealth.HealthDepleted += () => Log("[Player] ☠️ HealthDepleted!");
            _playerHealth.HealthRestored += () => Log("[Player] 💚 HealthRestored!");
        }

        if (_enemy1Health is { })
        {
            _enemy1Health.HealthChanged += (current, max) =>
                Log($"[Goblin] HealthChanged: {current}/{max}");
            _enemy1Health.HealthDecreased += (amount, current, source) =>
                Log($"[Goblin] HealthDecreased: -{amount} → {current}");
            _enemy1Health.HealthIncreased += (amount, current) =>
                Log($"[Goblin] HealthIncreased: +{amount} → {current}");
            _enemy1Health.HealthDepleted += () => Log("[Goblin] ☠️ HealthDepleted!");
            _enemy1Health.HealthRestored += () => Log("[Goblin] 💚 HealthRestored!");
        }

        if (_enemy2Health is { })
        {
            _enemy2Health.HealthChanged += (current, max) =>
                Log($"[Orc] HealthChanged: {current}/{max}");
            _enemy2Health.HealthDecreased += (amount, current, source) =>
                Log($"[Orc] HealthDecreased: -{amount} → {current}");
            _enemy2Health.HealthIncreased += (amount, current) =>
                Log($"[Orc] HealthIncreased: +{amount} → {current}");
            _enemy2Health.HealthDepleted += () => Log("[Orc] ☠️ HealthDepleted!");
            _enemy2Health.HealthRestored += () => Log("[Orc] 💚 HealthRestored!");
        }
    }

    private void Log(string message)
    {
        if (_eventLog is null)
        {
            return;
        }

        _eventLog.AppendText($"[color=gray]{message}[/color]\n");
    }

    private void UpdatePlayerInfo()
    {
        if (_playerInfoLabel is null || _playerHealth is null)
        {
            return;
        }

        var data = _playerHealth.Serialize();
        var current = (float)data["current_health"];
        var max = (float)data["max_health"];
        var dead = data["is_dead"].As<bool>();
        var fraction = _playerHealth.HealthFraction * 100f;
        _playerInfoLabel.Text =
            $"HP: {current:F0} / {max:F0} ({fraction:F1}%)  {(dead ? "[DEAD]" : "")}";
    }
}
