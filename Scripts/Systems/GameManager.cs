using Godot;
using System;

public partial class GameManager : Node
{
    public static GameManager Instance { get; private set; }

    //Slot activo y datos de la partida actual
    public int ActiveNodeId { get; set; } = -1;
    public SaveData ActiveSave { get; private set; }
    public RunMapNode[] CurrentRunMap => ActiveSave?.MapNodes ?? Array.Empty<RunMapNode>();
    public bool IsRunComplete => ActiveSave?.IsRunComplete ?? false;
    public int CurrentRunNodeId => ActiveSave?.CurrentNodeId ?? -1;
    public bool ActiveNodeIsFinal { get; set; }

    //Dificultad
    public float ActiveNodeDifficulty { get; set; } = 1.0f;

#if DEBUG
    public bool GodModeEnabled { get; set; } = false;
#endif

    public override void _Ready()
    {
        Instance = this;
    }

    public override void _Process(double delta)
    {
        if (ActiveSave != null)
        {
            ActiveSave.PlaytimeSeconds += (float)delta;
        }
    }

    public void ClearActiveSave()
    {
        ActiveSave = null;
    }

    //Carga la run existente
    public void LoadGame()
    {
        ActiveSave = SaveSystem.LoadRun();
        EnsureRunMap();
        EnsureCurrentRunNode();
    }

    //Crea una nueva run
    public void NewGame(string saveName)
    {
        ActiveSave = new SaveData
        {
            Name = saveName,
            MapSeed = unchecked((int)GD.Randi()),
            MapVersion = RunMapGenerator.CurrentMapVersion
        };
        EnsureRunMap();
        SaveSystem.SaveRun(ActiveSave);
    }

    //Guarda el estado actual
    public void SaveGame()
    {
        if (ActiveSave == null) return;
        SaveSystem.SaveRun(ActiveSave);
    }

    public Vector2 GetNodeMapCameraPosition()
    {
        if (ActiveSave == null)
            return Vector2.Zero;

        return new Vector2(ActiveSave.CameraPositionX, ActiveSave.CameraPositionY);
    }

    public void SaveNodeMapCameraPosition(Vector2 position)
    {
        if (ActiveSave == null)
            return;

        ActiveSave.CameraPositionX = position.X;
        ActiveSave.CameraPositionY = position.Y;
        SaveGame();
    }

    public void EnsureRunMap()
    {
        if (ActiveSave == null)
            return;

        if (ActiveSave.MapVersion == RunMapGenerator.CurrentMapVersion && RunMapGenerator.IsValid(ActiveSave.MapNodes))
            return;

        ActiveSave.MapNodes = RunMapGenerator.Generate(ActiveSave.MapSeed);
        ActiveSave.MapVersion = RunMapGenerator.CurrentMapVersion;
        ActiveSave.CompletedNodes = Array.Empty<int>();
        ActiveSave.CurrentNodeId = -1;
        SaveGame();
    }

    private void EnsureCurrentRunNode()
    {
        if (ActiveSave == null || ActiveSave.CurrentNodeId != -1)
            return;

        int latestLayer = -1;
        int latestNodeId = -1;
        foreach (RunMapNode node in ActiveSave.MapNodes)
        {
            if (IsNodeCompleted(node.Id) && node.Layer > latestLayer)
            {
                latestLayer = node.Layer;
                latestNodeId = node.Id;
            }
        }

        if (latestNodeId != -1)
        {
            ActiveSave.CurrentNodeId = latestNodeId;
            SaveGame();
        }
    }

    //Marca un nodo como completado y guarda
    public void CompleteNode(int nodeId)
    {
        if (ActiveSave == null) return;

        //Verifica que el nodo no esté ya completado
        foreach (int id in ActiveSave.CompletedNodes)
            if (id == nodeId) return;

        //Agrega el nodo completado al array
        int[] newCompleted = new int[ActiveSave.CompletedNodes.Length + 1];
        ActiveSave.CompletedNodes.CopyTo(newCompleted, 0);
        newCompleted[newCompleted.Length - 1] = nodeId;
        ActiveSave.CompletedNodes = newCompleted;
        ActiveSave.CurrentNodeId = nodeId;

        SaveGame();
    }

    public void SavePlayerState(Player player)
    {
        if (ActiveSave == null) return;
        
        ActiveSave.PlayerLevel = player.Experience.CurrentLevel;
        ActiveSave.PlayerXP = player.Experience.CurrentXP;
        ActiveSave.PlayerCurrentHealth = player.Health.CurrentHealth;
        ActiveSave.PlayerDashCooldown = player.Stats.DashCooldown;
        
        ActiveSave.AcquiredUpgrades.Clear();
        foreach (var upgrade in player.Upgrades.AcquiredUpgrades)
        {
            ActiveSave.AcquiredUpgrades[upgrade.Id] = upgrade.TimesApplied;
        }
        SaveGame();
    }

    public void CompleteRun()
    {
        if (ActiveSave == null)
            return;

        ActiveSave.IsRunComplete = true;
        SaveGame();
    }

    //Devuelve true si un nodo está completado
    public bool IsNodeCompleted(int nodeId)
    {
        if (ActiveSave == null) return false;
        foreach (int id in ActiveSave.CompletedNodes)
            if (id == nodeId) return true;
        return false;
    }
}