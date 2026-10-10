using System;
using Godot;

public partial class EnemySpawner : Node
{
    [Export] public PackedScene NightmareScene { get; set; }
    [Export] public PackedScene RangeNightmareScene { get; set; }
    [Export] public PackedScene FlyingNightmareScene { get; set; }

    //Cada cuantos segundos aparece un enemigo al inicio
    [Export] public float SpawnInterval = 1.5f;

    //Cuánto se reduce el intervalo de tiempo por nivel de dificultad
    [Export] public float SpawnIntervalDecrement = 0.02f;

    //Cuántos enemigos adicionales aparecen a la vez por nivel de dificultad
    [Export] public float EnemiesPerDifficultyLevel = 0.25f;

    //Intervalo mínimo, ya que no tendría sentido que llegue a 0
    [Export] public float MinSpawnInterval = 0.3f;

    //Radio desde el el jugador en donde aparecen los enemigos
    [Export] public float SpawnRadius = 700f;

    //Cada cuantos segundos aumenta la dificultad
    [Export] public float DifficultyInterval = 10f;
    [Export] public int MaxEnemies = 80;

    public bool IsPaused = false;

    private float _spawnTimer = 0f;
    private float _difficultyTimer = 0f;
    private int _difficultyLevel = 0;
    private Player _player;

    public override void _Ready()
    {
        _player = GetTree().GetFirstNodeInGroup("player") as Player;
    }

    public override void _Process(double delta)
    {
        if (_player == null) return;
        if (IsPaused) return; //No spawnean enemigos si está pousado

        _spawnTimer += (float)delta;
        _difficultyTimer += (float)delta;

        //Cada DifficultyInterval segundos aumentamos la dificultad
        if (_difficultyTimer >= DifficultyInterval)
        {
            _difficultyTimer = 0f;
            _difficultyLevel++;

            //Avisa al HUD
            HUD hud = GetTree().Root.FindChild("HUD", true, false) as HUD;
            hud?.UpdateDifficulty(_difficultyLevel);
        }

        // El intervalo de spawn se reduce según el nivel de dificultad actual
        float currentSpawnInterval = Mathf.Max(MinSpawnInterval, SpawnInterval - (_difficultyLevel * SpawnIntervalDecrement));

        if (_spawnTimer >= currentSpawnInterval)
        {
            _spawnTimer = 0f;

            // Calculamos cuántos enemigos spawnear a la vez según la dificultad
            int enemiesToSpawn = 1 + (int)(_difficultyLevel * EnemiesPerDifficultyLevel);
            int currentEnemies = GetTree().GetNodesInGroup("enemies").Count;

            for (int i = 0; i < enemiesToSpawn; i++)
            {
                if (currentEnemies < MaxEnemies)
                {
                    SpawnEnemy();
                    currentEnemies++;
                }
            }
        }
    }

    private void SpawnEnemy()
    {
        float roll = GD.Randf();
        PackedScene scene = roll < 0.5f
            ? NightmareScene
            : roll < 0.8f
                ? RangeNightmareScene
                : FlyingNightmareScene;
        Enemy enemy = scene.Instantiate<Enemy>();

        //Busca el contenedor de enemigos
        Node enemyContainer = GetTree().Root.FindChild("Enemies", true, false);
        enemyContainer.AddChild(enemy);

        //Se posiciona al enemigo en un punto aleatorio al rededor del jugador, fuera de su vista
        enemy.GlobalPosition = GetSpawnPosition();

        //Aplica el escalado de dificultad al enemigo recién creado
        ApplyDifficultyScaling(enemy);
    }

    private void ApplyDifficultyScaling(Enemy enemy)
    {
        //El multiplicador base viene del nodo activo en el GameManager
        float nodeDifficulty = GameManager.Instance.ActiveNodeDifficulty;

        //Se aplica el multiplicador del nodo sobre el escalado progresivo
        float healthMultiplier = 1.0f + ((nodeDifficulty - 1.0f) * 0.5f) + (_difficultyLevel * 0.08f);
        // Reducimos el impacto de la dificultad del mapa sobre la velocidad
        float speedMultiplier = 1.0f + ((nodeDifficulty - 1.0f) * 0.2f) + (_difficultyLevel * 0.005f);

        enemy.ScaleStats(healthMultiplier, speedMultiplier);
    }

    private Vector2 GetSpawnPosition()
    {
        Vector2 spawnPos = Vector2.Zero;
        bool positionFound = false;

        // Obtenemos el estado de las físicas del mundo para revisar colisiones
        var spaceState = _player.GetWorld2D().DirectSpaceState;

        // Intentamos hasta 30 veces encontrar una posición válida dentro del mapa
        for (int i = 0; i < 30; i++)
        {
            //Se elije un ángulo aleatorio en el círculo alrededor del jugador
            float angle = (float)GD.RandRange(0, Mathf.Tau);
            Vector2 offset = new(Mathf.Cos(angle) * SpawnRadius, Mathf.Sin(angle) * SpawnRadius);
            spawnPos = _player.GlobalPosition + offset;

            //Se limita la posición de forma lógica (el mapa va de -2400 a 2400)
            if (spawnPos.X >= -2400f && spawnPos.X <= 2400f && spawnPos.Y >= -2400f && spawnPos.Y <= 2400f)
            {
                // Configuramos una consulta de punto para ver si hay una colisión ahí
                var query = new PhysicsPointQueryParameters2D
                {
                    Position = spawnPos
                };

                // Si no hay intersecciones, significa que el área está libre
                var result = spaceState.IntersectPoint(query);
                if (result.Count == 0)
                {
                    positionFound = true;
                    break;
                }
            }
        }

        // Si después de 30 intentos no encontramos lugar libre, forzamos un spawn hacia el centro
        if (!positionFound)
        {
            Vector2 dirToCenter = (Vector2.Zero - _player.GlobalPosition).Normalized();
            spawnPos = _player.GlobalPosition + (dirToCenter * SpawnRadius);
        }

        return spawnPos;
    }
}
