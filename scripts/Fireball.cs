using Godot;
using System.Collections.Generic;

public partial class Fireball : Area3D
{
    public float Speed = 10.0f;
    public float Damage = 20.0f;
    public float ExplosionRadius = 3.0f;

    private Vector3 _targetPosition;
    private bool _isExploding = false;

    public void Setup(Vector3 target, float damage)
    {
        _targetPosition = target;
        Damage = damage;

        // Поворачиваем шар в сторону цели
        LookAt(target, Vector3.Up);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_isExploding) return;

        // Летим к цели
        Vector3 dir = (_targetPosition - GlobalPosition).Normalized();
        GlobalPosition += dir * Speed * (float)delta;

        // Если долетели или врезались
        if (GlobalPosition.DistanceTo(_targetPosition) < 0.5f)
        {
            Explode();
        }
    }

    private void Explode()
    {
        _isExploding = true;
        GetNode<MeshInstance3D>("MeshInstance3D").Visible = false; // Прячем шар
        GetNode<CollisionShape3D>("CollisionShape3D").Disabled = true;

        GD.Print("Взрыв фаербола!");

        // Находим всех врагов в радиусе взрыва
        var space = GetWorld3D().DirectSpaceState;
        var query = new PhysicsShapeQueryParameters3D();
        var shape = new SphereShape3D { Radius = ExplosionRadius };
        query.Shape = shape;
        query.Transform = new Transform3D(Basis.Identity, GlobalPosition);

        var results = space.IntersectShape(query);

        foreach (var result in results)
        {
            var collider = result["collider"].As<Node>();
            if (collider is Enemy enemy)
            {
                enemy.TakeDamage(Damage);
            }
        }

        // Удаляем сам фаербол через мгновение
        QueueFree();
    }
}