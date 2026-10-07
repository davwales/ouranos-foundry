using Ouranos.Foundry.Features.Health.Nodes;

namespace Ouranos.Foundry.Features.Health.Utils;

internal sealed class HealthMonitorConnection
{
    private HealthComponent? _component;
    private HealthComponent.HealthChangedEventHandler? _healthChangedHandler;
    private HealthComponent.HealthDecreasedEventHandler? _healthDecreasedHandler;
    private HealthComponent.MaxHealthChangedEventHandler? _maxHealthChangedHandler;

    public HealthComponent? Component => _component;

    public void Monitor(
        HealthComponent? newComponent,
        Action<float, float> onHealthChanged,
        Action<float, float, Node?>? onHealthDecreased = null,
        Action<float>? onMaxHealthChanged = null
    )
    {
        if (_component == newComponent)
        {
            return;
        }

        Disconnect();

        _component = newComponent;

        if (_component is null)
        {
            return;
        }

        _healthChangedHandler = (current, max) => onHealthChanged(current, max);
        _component.HealthChanged += _healthChangedHandler;

        if (onHealthDecreased is not null)
        {
            _healthDecreasedHandler = (amount, current, source) =>
                onHealthDecreased(amount, current, source);
            _component.HealthDecreased += _healthDecreasedHandler;
        }

        if (onMaxHealthChanged is not null)
        {
            _maxHealthChangedHandler = max => onMaxHealthChanged(max);
            _component.MaxHealthChanged += _maxHealthChangedHandler;
        }
    }

    public void Disconnect()
    {
        if (_component is null)
        {
            return;
        }

        if (_healthChangedHandler is not null)
        {
            _component.HealthChanged -= _healthChangedHandler;
        }

        if (_healthDecreasedHandler is not null)
        {
            _component.HealthDecreased -= _healthDecreasedHandler;
        }

        if (_maxHealthChangedHandler is not null)
        {
            _component.MaxHealthChanged -= _maxHealthChangedHandler;
        }

        _component = null;
        _healthChangedHandler = null;
        _healthDecreasedHandler = null;
        _maxHealthChangedHandler = null;
    }
}
