using Godot;

public partial class Unit : CharacterBody3D
{
    [Export] public float MoveSpeed { get; set; } = 4.0f;
    [Export] public float AttackRange { get; set; } = 1.5f;
    [Export] public float AttackDamage { get; set; } = 15.0f;
    [Export] public float AttackCooldown { get; set; } = 1.0f;

    private Vector3 _targetPosition;
    private bool _moving = false;
    private MeshInstance3D _mesh;
    private Health _health;

    private Enemy _targetEnemy;
    private float _attackTimer = 0f;

    public override void _Ready()
    {
        _targetPosition = GlobalPosition;
        _mesh = GetNode<MeshInstance3D>("MeshInstance3D");
        _health = GetNode<Health>("Health");
    }

    public override void _PhysicsProcess(double delta)
    {
        _attackTimer -= (float)delta;

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
                    _attackTimer = AttackCooldown;
                    float finalDamage = AttackDamage;

                    if (Inventory.Instance != null && Inventory.Instance.EquippedWeapon != null)
                    {
                        finalDamage += Inventory.Instance.EquippedWeapon.DamageBonus;
                    }

                    _targetEnemy.TakeDamage(finalDamage);

                    // Небольшой визуальный рывок к врагу при ударе
                    GlobalPosition += toEnemy.Normalized() * 0.2f;
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
        Velocity = dir.Normalized() * MoveSpeed;
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
        if (_mesh == null) return;
        var mat = _mesh.GetSurfaceOverrideMaterial(0) as StandardMaterial3D;
        if (mat == null)
        {
            mat = new StandardMaterial3D();
            _mesh.SetSurfaceOverrideMaterial(0, mat);
        }
        mat.AlbedoColor = value ? new Color(1f, 0.8f, 0.2f) : new Color(0.6f, 0.6f, 0.9f);
    }

    public void TakeDamage(float amount)
    {
        _health?.TakeDamage(amount);
    }
}