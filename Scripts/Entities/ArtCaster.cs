using System.IO;
using System.Runtime.ConstrainedExecution;
using Godot;

public partial class ArtCaster : Node
{
    [Export] public PackedScene FireballScene { get; set; }
    [Export] public PackedScene WaterBoltScene { get; set; }
    [Export] public PackedScene FrostRayScene { get; set; }
    [Export] public PackedScene ExplosionEffectScene { get; set; }
    [Export] public PackedScene RepulsionBurstScene { get; set; }
    [Export] public PackedScene ShockwaveScene { get; set; }

    [Export] public float FireballFireRate = 1f;
    [Export] public float WaterBoltFireRate = 1.5f;
    [Export] public float FrostRayFireRate = 2f;

    private float _fireballTimer = 0f;
    private float _waterBoltTimer = 0f;
    private float _frostRayTimer = 0f;
    private float _repulsionBurstTimer = 0f;
    private bool _isBurstFiring = false; //Evita que se superpongan ráfagas concurrentes

    private PlayerStats _stats;
    private Player _player;

    public void Initialize(Player player, PlayerStats stats)
    {
        _player = player;
        _stats = stats;
    }

    public void Process(float delta)
    {
        HandleFireball(delta);
        HandleWaterBolt(delta);
        HandleFrostRay(delta);
        HandleRepulsionBurst(delta);
    }

    private void HandleFireball(float delta)
    {
        //Si hay una ráfaga en progreso, no se dispara otra
        if (_isBurstFiring) return;

        _fireballTimer += delta;

        if (_fireballTimer >= FireballFireRate)
        {
            _fireballTimer = 0f;
            ShootFireball();
        }
    }

    private void HandleFrostRay(float delta)
    {
        //Si el jugador no tiene el rayo de hielo, no hacemos nada
        if (!_stats.HasFrostRay) return;

        _frostRayTimer += delta;

        if (_frostRayTimer >= FrostRayFireRate)
        {
            //Solo se resetea el timer si se disparó
            bool didShoot = ShootFrostRay();
            if (didShoot)
                _frostRayTimer = 0f;
        }
    }

    private void HandleWaterBolt(float delta)
    {
        if (!_stats.HasWaterBolt) return;

        _waterBoltTimer += delta;
        if (_waterBoltTimer < WaterBoltFireRate) return;

        Node2D nearest = FindNearestEnemy(_stats.WaterBoltRange);
        if (nearest == null) return;

        _waterBoltTimer = 0f;
        ShootWaterBolt(nearest);
    }

    private void ShootWaterBolt(Node2D target)
    {
        WaterBolt waterBolt = WaterBoltScene.Instantiate<WaterBolt>();
        GetProjectileContainer().AddChild(waterBolt);
        AudioManager.Instance.PlaySfx("sfx.waterbolt");
        waterBolt.GlobalPosition = _player.GlobalPosition;
        waterBolt.Direction = _player.GlobalPosition.DirectionTo(target.GlobalPosition);
        waterBolt.ElementType = Element.Water;
        waterBolt.Damage += _stats.BonusWaterBoltDamage + _stats.BonusDamage;
    }

    private void HandleRepulsionBurst(float delta)
    {
        if (!_stats.HasRepulsionBurst) return;

        _repulsionBurstTimer += delta;

        if (_repulsionBurstTimer >= _stats.RepulsionBurstFireRate)
        {
            _repulsionBurstTimer = 0f;
            SpawnRepulsionBurst();
        }
    }

    private void ShootFireball()
    {
        Node2D nearest = FindNearestEnemy(_stats.FireballRange);
        if (nearest == null) return;

        Vector2 baseDirection = _player.GlobalPosition.DirectionTo(nearest.GlobalPosition);

        //Si hay burst fire activo, dispara en ráfaga secuencial (misma dirección)
        if (_stats.FireballBurstCount > 0)
        {
            ShootBurstFire(baseDirection);
            return;
        }

        //Dispara tantos proyectiles como FireballCount lo indique (en abanico)
        for (int i = 0; i < _stats.FireballCount; i++)
        {
            SpawnSingleFireball(_player.GlobalPosition, GetSpreadDirection(baseDirection, i, _stats.FireballCount));
        }
    }

    private async void ShootBurstFire(Vector2 direction)
    {
        _isBurstFiring = true;

        int totalShots = _stats.FireballBurstCount;
        float burstDelay = 0.1f; //Delay entre cada disparo de la ráfaga

        for (int i = 0; i < totalShots; i++)
        {
            //Si el jugador fue eliminado durante la ráfaga, se detiene
            if (!IsInstanceValid(_player)) break;

            SpawnSingleFireball(_player.GlobalPosition, direction);

            //Espera un pequeño delay antes del siguiente disparo (excepto en el último)
            if (i < totalShots - 1)
                await _player.ToSignal(_player.GetTree().CreateTimer(burstDelay), "timeout");
        }

        _isBurstFiring = false;
    }

    private void SpawnSingleFireball(Vector2 position, Vector2 direction)
    {
        Fireball fireball = FireballScene.Instantiate<Fireball>();
        GetProjectileContainer().AddChild(fireball);
        AudioManager.Instance.PlaySfx("sfx.fireball");
        fireball.GlobalPosition = position;
        fireball.Direction = direction;

        fireball.Damage += _stats.BonusDamage + _stats.BonusFireballDamage;
        //Si tiene burst fire, el daño total se reduce un 15%
        if (_stats.FireballBurstCount > 0)
            fireball.Damage *= 0.85f;

        fireball.IsExplosive = _stats.FireballExplosive;
        fireball.ExplosionEffectScene = ExplosionEffectScene;
        fireball.IsPiercing = _stats.FireballPiercing;
    }

    private bool ShootFrostRay()
    {
        Node2D nearest = FindNearestEnemy(_stats.FrostRayRange);
        //Si no hay enemigos en el rango, no se dispara
        if (nearest == null) return false;

        FrostRay ray = FrostRayScene.Instantiate<FrostRay>();
        GetProjectileContainer().AddChild(ray);
        AudioManager.Instance.PlaySfx("sfx.frostray");

        //Posiciona el rayo en el jugador
        ray.GlobalPosition = _player.GlobalPosition;

        //Rota el rayo para que apunte hacia el enemigo
        Vector2 direction = _player.GlobalPosition.DirectionTo(nearest.GlobalPosition);
        ray.Rotation = direction.Angle();

        ray.Damage += _stats.BonusFrostRayDamage + _stats.BonusDamage;

        // FrozenEffect desactivado temporalmente: FrostRay solo hace daño.
        ray.IsChaining = _stats.FrostRayChain;

        //Se actualiza el ancho del rayo según los stats
        var shape = ray.GetNode<CollisionShape2D>("CollisionShape2D");
        if (shape.Shape is RectangleShape2D rect)
            rect.Size = new Vector2(rect.Size.X, _stats.FrostRayWidth);

        //También se actualiza el ColorRect
        var colorRect = ray.GetNode<ColorRect>("ColorRect");
        colorRect.Size = new Vector2(colorRect.Size.X, _stats.FrostRayWidth);
        colorRect.Position = new Vector2(0, -_stats.FrostRayWidth / 2f);

        return true; //Se disparó exitosamente
    }

    private void SpawnRepulsionBurst()
    {
        RepulsionBurst burst = RepulsionBurstScene.Instantiate<RepulsionBurst>();
        GetProjectileContainer().AddChild(burst);
        AudioManager.Instance.PlaySfx("sfx.repulsionburst");
        burst.GlobalPosition = _player.GlobalPosition;
        burst.Damage += _stats.BonusRepulsionBurstDamage + _stats.BonusDamage;
        burst.Force = _stats.RepulsionBurstForce;
        burst.Radius = _stats.RepulsionBurstRange;

        //Se actualiza el CollisionShape para que coincida con el radio
        var shape = burst.GetNode<CollisionShape2D>("CollisionShape2D");
        if (shape.Shape is CircleShape2D circle)
            circle.Radius = _stats.RepulsionBurstRange;
        
        burst.HasShockwave = _stats.RepulsionBurstShockwave;
        burst.ShockwaveScene = ShockwaveScene;
    }

    private Node2D FindNearestEnemy(float range)
    {
        //Busca a todos los enemigos en la escena
        var enemies = _player.GetTree().GetNodesInGroup("enemies");
        if (enemies.Count == 0) return null;

        //Encuentra al más cercano
        Node2D nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (var node in enemies)
        {
            if (node is Node2D enemy) //Si "node" es de tipo "Node2D", entonces crea un "Node2D" llamado "enemy" (o también se podría leer como "si node es de tipo "Node2D", cámbiale el nombre a "enemy" y déjame trabajar con él dentro de este bloque")
            {
                float distance = _player.GlobalPosition.DistanceTo(enemy.GlobalPosition);
                if (distance < nearestDistance && distance <= range)
                {
                    nearestDistance = distance;
                    nearest = enemy;
                }
            }
        }

        return nearest;
    }

    private Node GetProjectileContainer()
    {
        return _player.GetTree().Root.FindChild("Projectiles", true, false);
    }

    //Distribuye los proyectiles en abanico cuando hay más de uno
    private static Vector2 GetSpreadDirection(Vector2 baseDirection, int index, int total)
    {
        if (total == 1) return baseDirection;

        //Ángulo total del abanico en radianes (30 grados)
        float spreadAngle = Mathf.DegToRad(30f);

        //Se calcula el ángulo de este proyectil específico, los proyectiles se distribuyen simétricamente alrededor del centro
        float angleStep = spreadAngle / (total - 1);
        float angle = -spreadAngle / 2f + angleStep * index;

        //Se rota la dirección base por ese ángulo
        return baseDirection.Rotated(angle);
    }

    public float GetFrostRayTimerRemaining() => Mathf.Max(0f, FrostRayFireRate - _frostRayTimer);
}