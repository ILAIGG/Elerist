using Godot;

public partial class AbilityManager : Node
{
    [Export] public PackedScene FireNovaScene { get; set; }
    [Export] public PackedScene MeteorShowerScene { get; set; }
    [Export] public PackedScene MeteorIndicatorScene { get; set; }

    private float _fireNovaCooldownTimer = 0f;
    private float _meteorShowerCooldownTimer = 0f;

    private PlayerStats _stats;
    private Player _player;

    public void Initialize(Player player, PlayerStats stats)
    {
        _player = player;
        _stats = stats;
    }

    public void Process(float delta)
    {
        HandleFireNova(delta);
        HandleMeteorShower(delta);
    }

    private void HandleFireNova(float delta)
    {
        if (_fireNovaCooldownTimer > 0f)
            _fireNovaCooldownTimer -= delta;
        
        if (Input.IsActionJustPressed("ability_q") && _fireNovaCooldownTimer <= 0f)
        {
            SpawnFireNova();
            _fireNovaCooldownTimer = _stats.FireNovaCooldown;
        }
    }

    private void SpawnFireNova()
    {
        FireNova fireNova = FireNovaScene.Instantiate<FireNova>();
        GetProjectilesContainer().AddChild(fireNova);
        fireNova.GlobalPosition = _player.GlobalPosition;
        fireNova.Damage += _stats.BonusFireNovaDamage + _stats.BonusDamage;

        //Si tiene NovaChain, entonces se disparan las 3 bolas de fuego al explotar
        if (_stats.FireNovaChain)
            SpawnFireNovaChainFireballs();
    }

    private void SpawnFireNovaChainFireballs()
    {
        var enemies = _player.GetTree().GetNodesInGroup("enemies");
        if (enemies.Count == 0) return;

        //Se ordenan los enemigos por distancia y se toman los 3 más cercanos.
        var sortedEnemies = new System.Collections.Generic.List<Node2D>();
        foreach (var node in enemies)
        {
            if (node is Node2D enemy)
                sortedEnemies.Add(enemy);
        }

        //Este bloque se lee así: "para comparar a y b, calcula la distancia de cada uno al jugador y compáralas"
        sortedEnemies.Sort((a, b) =>
            _player.GlobalPosition.DistanceTo(a.GlobalPosition)
            .CompareTo(_player.GlobalPosition.DistanceTo(b.GlobalPosition)));
        
        int count = Mathf.Min(3, sortedEnemies.Count);
        for (int i = 0; i < count; i++)
        {
            Fireball fireball = _player.ArtCaster.FireballScene.Instantiate<Fireball>();
            GetProjectilesContainer().AddChild(fireball);
            fireball.GlobalPosition = _player.GlobalPosition;
            fireball.Direction = _player.GlobalPosition.DirectionTo(sortedEnemies[i].GlobalPosition);
            fireball.Damage += _stats.BonusDamage + _stats.BonusFireballDamage;
        }
    }

    private void HandleMeteorShower(float delta)
    {
        if (_meteorShowerCooldownTimer > 0f)
            _meteorShowerCooldownTimer -= delta;
        
        if (Input.IsActionJustPressed("ability_e") && _meteorShowerCooldownTimer <= 0f)
        {
            //Obtiene la posición del cursor en el mundo
            Vector2 targetPosition = _player.GetGlobalMousePosition();
            SpawnMeteorShower(targetPosition);
            _meteorShowerCooldownTimer = _stats.MeteorShowerCooldown;
        }
    }

    private async void SpawnMeteorShower(Vector2 targetPosition)
    {
        Node projectileContainer = GetProjectilesContainer();

        //Muestra el indicador de área
        if (MeteorIndicatorScene != null)
        {
            MeteorIndicator indicator = MeteorIndicatorScene.Instantiate<MeteorIndicator>();
            indicator.Radius = _stats.MeteorShowerSpread;
            projectileContainer.AddChild(indicator);
            indicator.GlobalPosition = targetPosition;
        }

        //Obtiene la posición de la cámara para calcular dónde caerán
        Camera2D camera = _player.GetNode<Camera2D>("Camera2D");
        Vector2 cameraPosition = camera.GlobalPosition;

        for (int i = 0; i < _stats.MeteorShowerCount; i++)
        {
            //Posición aleatoria dentro del radio de dispersión
            float angle = (float)GD.RandRange(0, Mathf.Tau);
            float distance = (float)GD.RandRange(0, _stats.MeteorShowerSpread);
            Vector2 offset = new(
                Mathf.Cos(angle) * distance,
                Mathf.Sin(angle) * distance
            );

            Meteor meteor = MeteorShowerScene.Instantiate<Meteor>();
            projectileContainer.AddChild(meteor);
            meteor.Damage += _stats.BonusMeteorShowerDamage + _stats.BonusDamage;

            //Se le dice al meteoro desde dónde cae y dónde impacta
            meteor.SetTarget(targetPosition + offset, cameraPosition);

            //Espera un poco entre cada meteoro para dar un efecto visual
            await ToSignal(_player.GetTree().CreateTimer(0.1f), "timeout");
        }
    }

    private Node GetProjectilesContainer()
    {
        return _player.GetTree().Root.FindChild("Projectiles", true, false);
    }

    public float GetNovaCooldownRemaining() => Mathf.Max(0f, _fireNovaCooldownTimer);
    public float GetMeteorShowerCooldownRemaining() => Mathf.Max(0f, _meteorShowerCooldownTimer);
}