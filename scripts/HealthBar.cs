using Godot;

public partial class HealthBar : Sprite3D
{
    private Health _health;
    private float _maxWidth;

    public override void _Ready()
    {
        _health = GetParent().GetNode<Health>("Health");
        _maxWidth = Scale.X; // Запоминаем изначальную ширину

        // Подключаемся к сигналу изменения здоровья
        _health.HealthChanged += OnHealthChanged;
        _health.Died += OnDied;
    }

    private void OnHealthChanged(float current, float max)
    {
        float percent = current / max;
        Scale = new Vector3(_maxWidth * percent, Scale.Y, Scale.Z);

        // Меняем цвет: зелёный -> жёлтый -> красный
        if (percent > 0.6f) Modulate = Colors.Green;
        else if (percent > 0.3f) Modulate = Colors.Yellow;
        else Modulate = Colors.Red;
    }

    private void OnDied()
    {
        QueueFree(); // Удаляем полоску при смерти
    }
}