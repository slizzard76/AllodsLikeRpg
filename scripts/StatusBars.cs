using Godot;

public partial class StatusBars : Node3D
{
    private Sprite3D _healthSprite;
    private Sprite3D _manaSprite;
    private Health _healthComponent;
    private Unit _unit;

    private float _maxWidth = 100f;

    public override void _Ready()
    {
        _healthSprite = GetNode<Sprite3D>("HealthSprite");
        _manaSprite = GetNode<Sprite3D>("ManaSprite");

        // Ищем компоненты у родителя (Unit)
        _healthComponent = GetParent().GetNode<Health>("Health");
        _unit = GetParent() as Unit;

        _maxWidth = _healthSprite.Scale.X; // Запоминаем ширину

        if (_healthComponent != null)
        {
            _healthComponent.HealthChanged += OnHealthChanged;
            OnHealthChanged(_healthComponent.CurrentHealth, _healthComponent.MaxHealth);
        }
    }

    public override void _Process(double delta)
    {
        // Обновляем ману каждый кадр
        if (_unit != null && _manaSprite != null)
        {
            float manaPercent = Mathf.Clamp(_unit.CurrentMana / _unit.MaxMana, 0, 1);
            _manaSprite.Scale = new Vector3(_maxWidth * manaPercent, _manaSprite.Scale.Y, _manaSprite.Scale.Z);
        }
    }

    private void OnHealthChanged(float current, float max)
    {
        float percent = Mathf.Clamp(current / max, 0, 1);
        _healthSprite.Scale = new Vector3(_maxWidth * percent, _healthSprite.Scale.Y, _healthSprite.Scale.Z);
    }
}