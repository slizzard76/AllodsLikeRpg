using Godot;

public partial class Health : Node
{
    [Export] public float MaxHealth { get; set; } = 100.0f;
    [Export] public float CurrentHealth { get; set; } = 100.0f;

    [Signal] public delegate void HealthChangedEventHandler(float current, float max);
    [Signal] public delegate void DiedEventHandler();

    [Export] public float RegenRate { get; set; } = 5.0f; // ХП в секунду
    private float _timeSinceLastDamage = 0f;
    public override void _Ready()
    {
        CurrentHealth = MaxHealth;
    }


    public override void _Process(double delta)
    {
        if (CurrentHealth <= 0 || CurrentHealth >= MaxHealth) return;

        _timeSinceLastDamage += (float)delta;
        if (_timeSinceLastDamage > 3.0f)
        {
            CurrentHealth = Mathf.Min(CurrentHealth + RegenRate * (float)delta, MaxHealth);
            // Обновляем полоску (сигнал можно не спамить каждый кадр, но для прототипа сойдёт)
        }
    }

    public void TakeDamage(float amount)
    {
        if (CurrentHealth <= 0) return;
        _timeSinceLastDamage = 0f; // Сбрасываем таймер регена
        CurrentHealth -= amount;
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        EmitSignal(SignalName.Died);

        var parent = GetParent();

        if (parent is Enemy enemy)
        {
            QuestManager.Instance.AddKillProgress("Очистка острова");


            foreach (Node node in enemy.GetParent().GetChildren())
            {
                if (node is Unit unit)
                {
                    unit.GainExperience(25); // Даем 25 опыта за врага
                }
            }

            // Спавн лута
            if (enemy.LootScene != null)
            {
                var loot = enemy.LootScene.Instantiate<LootItem>();
                enemy.GetParent().AddChild(loot);
                loot.GlobalPosition = enemy.GlobalPosition;

                // Случайно: 50 золота или Меч
                var rng = new RandomNumberGenerator();
                rng.Randomize();
                if (rng.Randf() > 0.5f)
                {
                    loot.ItemName = "Золото";
                    loot.ItemValue = rng.RandiRange(5, 15);
                    loot.ItemType = "Gold";
                }
                else
                {
                    loot.ItemName = "Ржавый меч";
                    loot.ItemValue = 10;
                    loot.ItemType = "Weapon";
                }

                // Добавляем лут в корень сцены (Main), чтобы он не удалился вместе с врагом
                
            }
        }

        parent.QueueFree();
    }
}