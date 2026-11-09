using Godot;
using System.Collections.Generic;

public partial class Enemy : CharacterBody3D
{

    [Export] public PackedScene LootScene { get; set; }

    [Export] public float PatrolSpeed { get; set; } = 2.0f;
    [Export] public float ChaseSpeed { get; set; } = 3.5f;
    [Export] public float AttackRange { get; set; } = 1.5f;
    [Export] public float AttackDamage { get; set; } = 10.0f;
    [Export] public float AttackCooldown { get; set; } = 1.2f;

    private enum State { Patrol, Chase, Attack }
    private State _state = State.Patrol;

    private Vector3 _patrolTarget;
    private float _patrolTimer = 0f;
    private Unit _targetUnit;
    private float _attackTimer = 0f;
    private Area3D _detectionArea;
    private Health _health;

    private float _stuckTimer = 0f;

    public override async void _Ready()
    {
        _detectionArea = GetNode<Area3D>("DetectArea");
        _detectionArea.BodyEntered += OnBodyEntered;
        _detectionArea.BodyExited += OnBodyExited;
        _health = GetNode<Health>("Health");

        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        PickNewPatrolTarget();
        
        
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is Unit unit && _targetUnit == null)
            _targetUnit = unit;
    }

    private void OnBodyExited(Node3D body)
    {
        if (body == _targetUnit)
            _targetUnit = null;
    }

    public override void _PhysicsProcess(double delta)
    {
        _attackTimer -= (float)delta;

        // Если цель умерла — ищем новую
        if (_targetUnit != null && !IsInstanceValid(_targetUnit))
            _targetUnit = null;

        switch (_state)
        {
            case State.Patrol: DoPatrol((float)delta); break;
            case State.Chase: DoChase(); break;
            case State.Attack: DoAttack(); break;
        }

        MoveAndSlide();
    }

    private void DoPatrol(float delta)
    {
        if (_targetUnit != null) { _state = State.Chase; return; }

        Vector3 dir = _patrolTarget - GlobalPosition;
        dir.Y = 0;

        if (dir.Length() < 0.5f)
        {
            // Дошли до точки
            _patrolTimer -= delta;
            if (_patrolTimer <= 0) PickNewPatrolTarget();
            Velocity = Vector3.Zero;
            _stuckTimer = 0;
        }
        else
        {
            Velocity = dir.Normalized() * PatrolSpeed;

            // Проверка на застревание (если скорость есть, но мы не двигаемся)
            if (Velocity.Length() > 0.1f && GetRealVelocity().Length() < 0.1f)
            {
                _stuckTimer += delta;
                if (_stuckTimer > 1.0f) // Застряли на секунду
                {
                    PickNewPatrolTarget();
                    _stuckTimer = 0;
                }
            }
            else
            {
                _stuckTimer = 0;
            }
        }
    }

    private void DoChase()
    {
        if (_targetUnit == null) { _state = State.Patrol; return; }

        Vector3 toTarget = _targetUnit.GlobalPosition - GlobalPosition;
        toTarget.Y = 0;
        float dist = toTarget.Length();

        if (dist <= AttackRange)
        {
            _state = State.Attack;
            Velocity = Vector3.Zero;
        }
        else Velocity = toTarget.Normalized() * ChaseSpeed;
    }

    private void DoAttack()
    {
        if (_targetUnit == null) { _state = State.Patrol; return; }

        Vector3 toTarget = _targetUnit.GlobalPosition - GlobalPosition;
        toTarget.Y = 0;

        // Если цель убежала — догоняем
        if (toTarget.Length() > AttackRange * 1.2f)
        {
            _state = State.Chase;
            return;
        }

        Velocity = Vector3.Zero;

        if (_attackTimer <= 0f)
        {
            _attackTimer = AttackCooldown;
            _targetUnit.TakeDamage(AttackDamage);
        }
    }

    private void PickNewPatrolTarget()
    {
        var rng = new RandomNumberGenerator();
        rng.Randomize();

        // Пытаемся найти точку в радиусе 4 метров от текущей позиции
        Vector3 randomOffset = new Vector3(
            rng.RandfRange(-4.0f, 4.0f),
            0,
            rng.RandfRange(-4.0f, 4.0f)
        );

        _patrolTarget = GlobalPosition + randomOffset;

        // Жёстко ограничиваем координаты островом (например, от -8 до 8)
        _patrolTarget.X = Mathf.Clamp(_patrolTarget.X, -8.0f, 8.0f);
        _patrolTarget.Z = Mathf.Clamp(_patrolTarget.Z, -8.0f, 8.0f);

        _patrolTimer = rng.RandfRange(1.5f, 3.0f);
    }

    public void TakeDamage(float amount)
    {
        _health?.TakeDamage(amount);
    }
}