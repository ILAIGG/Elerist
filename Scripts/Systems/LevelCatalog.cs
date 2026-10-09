using Godot;
using System;

public sealed class LevelDefinition
{
    public string Id { get; }
    public string DisplayName { get; }
    public PackedScene Scene { get; }
    public float BaseDifficulty { get; }

    public LevelDefinition(string id, string displayName, PackedScene scene, float baseDifficulty)
    {
        Id = id;
        DisplayName = displayName;
        Scene = scene;
        BaseDifficulty = baseDifficulty;
    }
}

public static class LevelCatalog
{
    public const string ForestWorldId = "Forest";

    private static readonly LevelDefinition[] ForestLevels =
    {
        new("Forest_1", "Forest 1", GD.Load<PackedScene>("res://Scenes/World/Level_Forest_1.tscn"), 1.0f),
        new("Forest_2", "Forest 2", GD.Load<PackedScene>("res://Scenes/World/Level_Forest_2.tscn"), 1.2f),
        new("Forest_3", "Forest 3", GD.Load<PackedScene>("res://Scenes/World/Level_Forest_3.tscn"), 1.4f),
        new("Forest_4", "Forest 4", GD.Load<PackedScene>("res://Scenes/World/Level_Forest_4.tscn"), 1.6f),
        new("Forest_5", "Forest 5", GD.Load<PackedScene>("res://Scenes/World/Level_Forest_5.tscn"), 1.8f)
    };

    private static readonly LevelDefinition ForestBossBattle = new(
        "Forest_BossBattle",
        "Boss Battle",
        GD.Load<PackedScene>("res://Scenes/World/Level_Forest_BossBattle.tscn"),
        1.0f);

    public static LevelDefinition[] GetWorldLevels(string worldId)
    {
        if (worldId != ForestWorldId)
            throw new ArgumentException($"Unknown world catalog: {worldId}");

        return ForestLevels;
    }

    public static LevelDefinition GetLevel(string levelId)
    {
        if (levelId == ForestBossBattle.Id)
            return ForestBossBattle;

        foreach (LevelDefinition level in ForestLevels)
        {
            if (level.Id == levelId)
                return level;
        }

        return null;
    }
}
