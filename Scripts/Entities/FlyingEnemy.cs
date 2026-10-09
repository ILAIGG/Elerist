using Godot;

public partial class FlyingEnemy : Enemy
{
    public override void _Ready()
    {
        base._Ready();
        SetCollisionMaskValue(5, false);
    }
}