using Godot;

public partial class Unit : CharacterBody3D
{
    [Export] public float MaxMana { get; set; } = 50.0f;
    [Export] public float CurrentMana { get; set; } = 50.0f;
    [Export] public float ManaRegen { get; set; } = 2.0f; // Восстановление в секунду
    public Enemy GetTargetEnemy() => _targetEnemy;
    [Export] public PackedScene FireballScene { get; set; }
    [Export] public float ManaCost { get; set; } = 10.0f; // Пока без маны, просто для галочки
    public float SpellCooldown = 2.0f;
    private float _spellTimer = 0f;

    // В самом начале класса Unit, рядом с другими Export-переменными:
    [Export] public int Level { get; set; } = 1;
    [Export] public int Experience { get; set; } = 0;
    [Export] public int StatPoints { get; set; } = 0; // Свободные очки

    [Export] public int Strength { get; set; } = 5;    // Влияет на урон
    [Export] public int Agility { get; set; } = 5;     // Влияет на скорость
    [Export] public int Intellect { get; set; } = 5;   // В будущем — на ману/способности

    // Опыт, необходимый для следующего уровня (например, 100 * уровень)
    public int ExpToNextLevel => Level * 100;


    [Export] public float BaseMoveSpeed { get; set; } = 4.0f;
    public float CurrentMoveSpeed => BaseMoveSpeed + (Agility * 0.1f);
    [Export] public float AttackRange { get; set; } = 1.5f;
    [Export] public float AttackDamage { get; set; } = 15.0f;
    [Export] public float AttackCooldown { get; set; } = 1.0f;
    public float CurrentAttackCooldown => Mathf.Max(0.2f, AttackCooldown - (Agility * 0.02f));

    private Vector3 _targetPosition;
    private bool _moving = false;
    private MeshInstance3D _mesh;
    private Health _health;

    private Enemy _targetEnemy;
    private float _attackTimer = 0f;

    private bool _isSelected;

    public override void _Ready()
    {
        _targetPosition = GlobalPosition;
        _mesh = GetNode<MeshInstance3D>("MeshInstance3D");
        _health = GetNode<Health>("Health");
        UpdateColor();
    }

    public override void _PhysicsProcess(double delta)
    {
        _attackTimer -= (float)delta;
        _spellTimer -= (float)delta;

        if (CurrentMana < MaxMana)
            CurrentMana = Mathf.Min(CurrentMana + ManaRegen * (float)delta, MaxMana);

        // Если цель-враг была уничтожена, сбрасываем её
        if (_targetEnemy != null && !IsInstanceValid(_targetEnemy))
        {
            _targetEnemy = null;
            _moving = false;
        }

        // --- ЛОГИКА БОЯ ---
        if (_targetEnemy != null && IsInstanceValid(_targetEnemy))
        {
            Vector3 toEnemy = _targetEnemy.GlobalPosition - GlobalPosition;
            toEnemy.Y = 0;

            if (toEnemy.Length() <= AttackRange)
            {
                // Враг в радиусе атаки — стоим и бьём
                Velocity = Vector3.Zero;
                if (_attackTimer <= 0f)
                {
                    _attackTimer = CurrentAttackCooldown;

                    // Базовый урон + бонус от Силы + бонус от оружия
                    float finalDamage = AttackDamage + (Strength * 2);

                    if (Inventory.Instance != null && Inventory.Instance.EquippedWeapon != null)
                    {
                        finalDamage += Inventory.Instance.EquippedWeapon.DamageBonus;
                    }

                    _targetEnemy.TakeDamage(finalDamage);
                }
            }
            else
            {
                // Идём к врагу
                MoveTowards(_targetEnemy.GlobalPosition);
            }
        }
        // --- ЛОГИКА ДВИЖЕНИЯ ПО КЛИКУ ---
        else if (_moving)
        {
            Vector3 dir = _targetPosition - GlobalPosition;
            dir.Y = 0.0f;

            if (dir.Length() < 0.2f)
            {
                // Дошли до точки
                _moving = false;
                Velocity = Vector3.Zero;
            }
            else
            {
                MoveTowards(_targetPosition);
            }
        }
        // --- СТОИМ НА МЕСТЕ ---
        else
        {
            Velocity = Vector3.Zero;
        }

        // Двигаем физическое тело (Godot сам решит, как скользить по стенам)
        MoveAndSlide();

        // ПРОВЕРКА НА ЗАСТРЕВАНИЕ
        // Если мы пытаемся двигаться (Velocity задан), но фактически стоим (GetRealVelocity близко к нулю)
        if (_moving && Velocity.Length() > 0.1f && GetRealVelocity().Length() < 0.1f)
        {
            // Намертво уперлись в стену — отменяем приказ
            _moving = false;
            Velocity = Vector3.Zero;
            GD.Print("Юнит застрял и отменил приказ движения.");
        }
    }

    // Вспомогательный метод: просто задаёт скорость в сторону цели
    private void MoveTowards(Vector3 destination)
    {
        Vector3 dir = destination - GlobalPosition;
        dir.Y = 0;
        Velocity = dir.Normalized() * CurrentMoveSpeed;
    }

    public void SetMoveTarget(Vector3 pos)
    {
        _targetPosition = pos;
        _moving = true;
        _targetEnemy = null; // Отменяем бой, если кликнули по земле
    }

    public void SetTargetEnemy(Enemy enemy)
    {
        _targetEnemy = enemy;
        _moving = true;
    }

    public void SetSelected(bool value)
    {
        _isSelected = value;
        var ring = GetNodeOrNull<MeshInstance3D>("SelectionRing");
        if (ring != null)
            ring.Visible = value;
    }

    public override void _Process(double delta)
    {
        if (_isSelected)
        {
            var ring = GetNodeOrNull<MeshInstance3D>("SelectionRing");
            if (ring != null)
                ring.RotateY((float)delta * 2.0f);
        }
    }

    public void TakeDamage(float amount)
    {
        _health?.TakeDamage(amount);
    }

    public void GainExperience(int amount)
    {
        int bonusXp = Mathf.RoundToInt(amount * (1.0f + Intellect * 0.05f));
        Experience += bonusXp;
        
        GD.Print($"Получено {amount} опыта. Всего: {Experience}/{ExpToNextLevel}");

        while (Experience >= ExpToNextLevel)
        {
            Experience -= ExpToNextLevel;
            LevelUp();
        }
    }

    private void LevelUp()
    {
        Level++;
        StatPoints += 3; // Даем 3 очка за уровень
        GD.Print($"НОВЫЙ УРОВЕНЬ: {Level}! Доступно очков: {StatPoints}");

        // При повышении уровня восстанавливаем здоровье
        var health = GetNode<Health>("Health");
        health.MaxHealth += 10; // Увеличиваем максимальное здоровье
        health.CurrentHealth = health.MaxHealth; // И лечим до полного
    }


    public void UpdateColor()
    {
        if (_mesh == null) return;

        // Не меняем цвет, если юнит выделен (желтый)
        if (_mesh.GetSurfaceOverrideMaterial(0) is StandardMaterial3D mat && mat.AlbedoColor == new Color(1f, 0.8f, 0.2f))
            return;

        Color color = new Color(0.6f, 0.6f, 0.9f); // Базовый (серо-синий)

        if (Strength >= Agility && Strength >= Intellect) color = new Color(0.8f, 0.2f, 0.2f); // Воин (Красный)
        else if (Agility >= Strength && Agility >= Intellect) color = new Color(0.2f, 0.8f, 0.2f); // Ловкач (Зеленый)
        else if (Intellect >= Strength && Intellect >= Agility) color = new Color(0.2f, 0.2f, 0.9f); // Маг (Синий)

        var material = _mesh.GetSurfaceOverrideMaterial(0) as StandardMaterial3D;
        if (material == null)
        {
            material = new StandardMaterial3D();
            _mesh.SetSurfaceOverrideMaterial(0, material);
        }
        material.AlbedoColor = color;
    }

    public void CastFireball(Enemy target)
    {
        if (FireballScene == null) return;
        if (target == null || !IsInstanceValid(target)) return;
        if (_spellTimer > 0) return;

        if (CurrentMana < 15.0f) // Стоимость фаербола
        {
            GD.Print("Недостаточно маны!");
            return;
        }

        _spellTimer = SpellCooldown;

        CurrentMana -= 15.0f; // Тратим ману
        // Создаем фаербол
        var fireball = FireballScene.Instantiate<Fireball>();

        // Наносим урон, зависящий от Интеллекта (например, Интеллект * 3 + базовый)
        float damage = 10 + (Intellect * 3);

        // Добавляем в мир и задаем позицию
        GetParent().AddChild(fireball);
        fireball.GlobalPosition = GlobalPosition + Vector3.Up * 1.0f;

        // Настраиваем полет к цели
        fireball.Setup(target.GlobalPosition, damage);

        GD.Print($"Каст фаербола! Урон: {damage}");
    }

}