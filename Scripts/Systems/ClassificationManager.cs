using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

public partial class ClassificationManager : Node
{
    private const string SavePath = "user://classification_scores.json";
    private const int MaximumEntries = 20;

    private sealed class ScoreEntry
    {
        public ScoreEntry() { }

        public string Name { get; set; } = string.Empty;
        public int Score { get; set; }
    }

    private readonly List<ScoreEntry> _entries = new();
    private string _playerName = string.Empty;
    private int _score;
    private int _lastRunScore;
    private bool _runActive;
    private bool _hasLastRunResult;

    public override void _Ready()
    {
        LoadEntries();
    }

    public void BeginRun(string playerName)
    {
        string cleanName = new((playerName ?? string.Empty).Trim().Where(character => !char.IsControl(character)).Take(32).ToArray());
        _playerName = string.IsNullOrWhiteSpace(cleanName)
            ? LocalizationManager.Translate("classification.default_name")
            : cleanName;
        _score = 0;
        _lastRunScore = 0;
        _runActive = true;
    }

    public void AddEnemyScore(int difficultyLevel, float nodeDifficulty)
    {
        float multiplier = 1f + Mathf.Max(0, difficultyLevel) * 0.1f + Mathf.Max(0f, nodeDifficulty - 1f) * 1.5f;
        AddPoints(Mathf.RoundToInt(10f * multiplier));
    }

    public void AddBossScore(float nodeDifficulty)
    {
        AddPoints(Mathf.RoundToInt(1000f * Mathf.Max(1f, nodeDifficulty)));
    }

    public void AddCompletedNodeScore(float nodeDifficulty)
    {
        AddPoints(Mathf.RoundToInt(100f * Mathf.Max(1f, nodeDifficulty)));
    }

    public void AddCardScore()
    {
        AddPoints(25);
    }

    public void FinishRun()
    {
        if (!_runActive)
            return;

        _lastRunScore = _score;
        _hasLastRunResult = true;
        _entries.Add(new ScoreEntry { Name = _playerName, Score = _score });
        SortAndTrimEntries();
        SaveEntries();
        _runActive = false;
    }

    public int GetLastRunScore()
    {
        return _lastRunScore;
    }

    public bool HasLastRunResult()
    {
        return _hasLastRunResult;
    }

    public string GetLastRunName()
    {
        return _playerName;
    }

    public string GetLeaderboardRows()
    {
        SortAndTrimEntries();
        return string.Join("\n", _entries.Select(entry => $"{entry.Name}\t{entry.Score}"));
    }

    private void AddPoints(int points)
    {
        if (_runActive)
            _score += Mathf.Max(0, points);
    }

    private void SortAndTrimEntries()
    {
        List<ScoreEntry> sorted = _entries
            .OrderByDescending(entry => entry.Score)
            .ToList();
        _entries.Clear();
        _entries.AddRange(sorted.Take(MaximumEntries));
    }

    private void LoadEntries()
    {
        if (!FileAccess.FileExists(SavePath))
            return;

        try
        {
            string json = FileAccess.GetFileAsString(SavePath);
            List<ScoreEntry> loaded = JsonSerializer.Deserialize<List<ScoreEntry>>(json);
            if (loaded != null)
                _entries.AddRange(loaded.Where(entry => entry != null && !string.IsNullOrWhiteSpace(entry.Name) && entry.Score >= 0));
            SortAndTrimEntries();
        }
        catch (JsonException exception)
        {
            GD.PushWarning($"Could not load classification scores: {exception.Message}");
        }
    }

    private void SaveEntries()
    {
        using FileAccess file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            GD.PushWarning("Could not save classification scores.");
            return;
        }

        file.StoreString(JsonSerializer.Serialize(_entries));
    }
}