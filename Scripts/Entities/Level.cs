using Godot;
using System.Collections.Generic;

public partial class Level : Node2D
{
    [Export] public PackedScene DamageNumberScene { get; set; }

    private bool _victoryAchieved = false;

    //Pausa
    private PackedScene _pauseMenuScene = GD.Load<PackedScene>("res://Scenes/UI/PauseMenu.tscn");
    private bool _isPaused = false;

    //Lista de condiciones de victoria detectadas como nodos hijos
    private List<VictoryCondition> _victoryConditions = new();
    private KillBossCondition _killBossCondition;
    private SurviveTimeCondition _surviveTimeCondition;
    private RunCompleted _runCompleted;

    //Hud
    private HUD _hud;

    //Tutorial
    [Export] public PackedScene TutorialDialogScene { get; set; }

    public override void _Ready()
    {
        DamageNumberSystem.Initialize(DamageNumberScene);

        // Busca todas las condiciones de victoria definidas como nodos hijos
        foreach (Node child in GetChildren())
        {
            if (child is VictoryCondition condition)
                _victoryConditions.Add(condition);
        }

        //Busca específicamente KillBossCondition para notificarle cuando spawnee el jefe
        _killBossCondition = GetNodeOrNull<KillBossCondition>("KillBossCondition");

        bool isFinalNode = GameManager.Instance.ActiveNodeIsFinal;
        if (_killBossCondition != null)
            _killBossCondition.Enabled = isFinalNode;

        _hud = GetTree().Root.FindChild("HUD", true, false) as HUD;
        _surviveTimeCondition = GetNodeOrNull<SurviveTimeCondition>("SurviveTimeCondition");

        if (_surviveTimeCondition != null)
            _surviveTimeCondition.Enabled = !isFinalNode;

        if (isFinalNode)
        {
            PackedScene runCompletedScene = GD.Load<PackedScene>("res://Scenes/UI/RunCompleted.tscn");
            _runCompleted = runCompletedScene.Instantiate<RunCompleted>();
            _runCompleted.ProcessMode = Node.ProcessModeEnum.Always;
            GetNode<CanvasLayer>("UI").AddChild(_runCompleted);

            BossSpawner bossSpawner = GetNodeOrNull<BossSpawner>("BossSpawner");
            if (bossSpawner != null)
            {
                EnemySpawner enemySpawner = GetNodeOrNull<EnemySpawner>("EnemySpawner");
                if (enemySpawner != null)
                    enemySpawner.IsPaused = true;

                bossSpawner.SpawnBoss();
                _killBossCondition?.NotifyBossSpawned();
            }
        }

        //Inicializa el objetivo en el HUD
        if (_surviveTimeCondition != null && _surviveTimeCondition.Enabled)
        {
            float remaining = _surviveTimeCondition.GetTimeRemaining();
            _hud?.UpdateObjectiveTime(remaining);
        }
        else if (_killBossCondition != null && _killBossCondition.Enabled)
        {
            _hud?.SetObjective("Defeat the Boss");
        }

        //Tutorial
        //Si hay un diálogo de tutorial configurado, lo instancia al inicio
        if (TutorialDialogScene != null)
        {
            TutorialDialog dialog = TutorialDialogScene.Instantiate<TutorialDialog>();
            GetNode<CanvasLayer>("UI").AddChild(dialog);
        }

        AudioManager.Instance.PlayMusic("r!ckes-creation.theme");
    }

    public override void _Process(double delta)
    {
        //Actualiza el objetivo de supervivencia en el HUD
        if (_surviveTimeCondition != null && _surviveTimeCondition.Enabled && !_victoryAchieved)
            _hud?.UpdateObjectiveTime(_surviveTimeCondition.GetTimeRemaining());

        if (_victoryAchieved) return;

        //Solo verifica condiciones si hay alguna definida
        if (_victoryConditions.Count > 0)
        {
            bool allCompleted = true;
            foreach (VictoryCondition condition in _victoryConditions)
            {
                // Ignora las condiciones deshabilitadas
                if (!condition.Enabled) continue;

                if (!condition.IsCompleted)
                {
                    allCompleted = false;
                    break;
                }
            }

            if (allCompleted)
            {
                _victoryAchieved = true;
                OnVictory();
                return;
            }
        }
    }

    private void OnVictory()
    {
        // Marca el nodo como completado en el GameManager
        GameManager.Instance.CompleteNode(GameManager.Instance.ActiveNodeId);

        Player player = GetTree().GetFirstNodeInGroup("player") as Player;
        if (player != null)
        {
            GameManager.Instance.SavePlayerState(player);
        }

        if (GameManager.Instance.ActiveNodeIsFinal)
            GameManager.Instance.CompleteRun();

        if (GameManager.Instance.ActiveNodeIsFinal)
            _runCompleted?.ShowRunCompleted();
        else
        {
            Victory victory = GetTree().Root.FindChild("Victory", true, false) as Victory;
            victory?.ShowVictory();
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel") && !_isPaused)
        {
            _isPaused = true;
            GetTree().Paused = true;

            PauseMenu pauseMenu = _pauseMenuScene.Instantiate<PauseMenu>();

            //Lo agrega al CanvasLayer de UI para que se dibuje encima de todo
            GetNode<CanvasLayer>("UI").AddChild(pauseMenu);

            //Cuando el PauseMenu se destruya, resetea _isPaused
            pauseMenu.TreeExited += () => _isPaused = false;
        }

#if DEBUG
        if (GameManager.Instance.GodModeEnabled && @event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            if (keyEvent.Keycode == Key.Kp0)
            {
                Player player = GetTree().GetFirstNodeInGroup("player") as Player;
                if (player != null)
                {
                    player.Experience.AddXP(player.Experience.XPToNextLevel);
                }
            }
            else if (keyEvent.Keycode == Key.Kp1)
            {
                Player player = GetTree().GetFirstNodeInGroup("player") as Player;
                if (player != null)
                {
                    player.Health.IsImmortal = !player.Health.IsImmortal;
                    GD.Print("Immortality " + (player.Health.IsImmortal ? "On" : "Off"));
                }
            }
            else if (keyEvent.Keycode == Key.Kp2)
            {
                if (!_victoryAchieved)
                {
                    _victoryAchieved = true;
                    OnVictory();
                }
            }
        }
#endif
    }
}