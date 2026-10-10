using Godot;

public partial class BossSpawner : Node2D
{
    [Export] public PackedScene BossScene { get; set; }

    public Boss SpawnBoss()
    {
        if (BossScene == null)
            return null;

        Node enemyContainer = GetTree().Root.FindChild("Enemies", true, false);
        if (enemyContainer == null)
            return null;

        Boss boss = BossScene.Instantiate<Boss>();
        enemyContainer.AddChild(boss);
        boss.GlobalPosition = GlobalPosition;
        return boss;
    }
}