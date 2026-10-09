using Godot;

public partial class EnemyProjectile : Area2D
{
    private Vector2 _direction;
    private float _speed;
    private float _damage;
    private ulong _sourceEntityId;
    private float _distanceTravelled;

    public void Initialize(Vector2 direction, float speed, float damage, ulong sourceEntityId)
    {
        _direction = direction.Normalized();
        _speed = speed;
        _damage = damage;
        _sourceEntityId = sourceEntityId;
    }

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    public override void _PhysicsProcess(double delta)
    {
        float movement = _speed * (float)delta;
        GlobalPosition += _direction * movement;
        _distanceTravelled += movement;

        if (_distanceTravelled >= 1000f)
            QueueFree();
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is not Player player)
            return;

        player.Health.TakeDamage(_damage, player.GlobalPosition, GetTree(), _sourceEntityId);
        QueueFree();
    }
}