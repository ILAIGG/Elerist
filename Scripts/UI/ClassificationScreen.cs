using Godot;

public partial class ClassificationScreen : Control
{
	private VBoxContainer _leaderboardRows;

	public override void _Ready()
	{
		GetNode<Label>("Content/Title").Text = LocalizationManager.Translate("classification.title");
		GetNode<Button>("Content/MainMenuButton").Text = LocalizationManager.Translate("classification.return_to_menu");
		GetNode<Button>("Content/MainMenuButton").Pressed += OnMainMenuPressed;

		GetNode<Label>("Content/LeaderboardScroll/Leaderboard/Header/RankHeader").Text = LocalizationManager.Translate("classification.rank");
		GetNode<Label>("Content/LeaderboardScroll/Leaderboard/Header/PlayerHeader").Text = LocalizationManager.Translate("classification.player");
		GetNode<Label>("Content/LeaderboardScroll/Leaderboard/Header/ScoreHeader").Text = LocalizationManager.Translate("classification.score");
		_leaderboardRows = GetNode<VBoxContainer>("Content/LeaderboardScroll/Leaderboard/Rows");
		PopulateLeaderboard();
		PopulateLastRun();
	}

	private void PopulateLeaderboard()
	{
		foreach (Node child in _leaderboardRows.GetChildren())
			child.QueueFree();

		Node manager = GetNodeOrNull<Node>("/root/ClassificationManager");
		string rows = manager?.Call("GetLeaderboardRows").AsString() ?? string.Empty;
		string[] lines = rows.Split('\n', System.StringSplitOptions.RemoveEmptyEntries);
		if (lines.Length == 0)
		{
			AddLeaderboardRow("-", LocalizationManager.Translate("classification.empty"), "-");
			return;
		}

		for (int index = 0; index < lines.Length; index++)
		{
			string[] columns = lines[index].Split('\t', 2);
			if (columns.Length != 2 || !int.TryParse(columns[1], out int score))
				continue;

			AddLeaderboardRow((index + 1).ToString(), columns[0], score.ToString());
		}
	}

	private void AddLeaderboardRow(string rank, string playerName, string score)
	{
		HBoxContainer row = new()
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			CustomMinimumSize = new Vector2(0, 30)
		};
		row.AddThemeConstantOverride("separation", 24);

		Label rankLabel = new()
		{
			Text = rank,
			CustomMinimumSize = new Vector2(72, 0),
			VerticalAlignment = VerticalAlignment.Center
		};
		Label playerLabel = new()
		{
			Text = playerName,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			VerticalAlignment = VerticalAlignment.Center,
			ClipText = true
		};
		Label scoreLabel = new()
		{
			Text = score,
			CustomMinimumSize = new Vector2(128, 0),
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Center
		};

		row.AddChild(rankLabel);
		row.AddChild(playerLabel);
		row.AddChild(scoreLabel);
		_leaderboardRows.AddChild(row);
		_leaderboardRows.AddChild(new HSeparator());
	}

	private void PopulateLastRun()
	{
		Node manager = GetNodeOrNull<Node>("/root/ClassificationManager");
		if (manager == null || !manager.Call("HasLastRunResult").AsBool())
			return;

		int score = (int)manager.Call("GetLastRunScore");
		string playerName = manager.Call("GetLastRunName").AsString();
		GetNode<Label>("Content/RunResult").Text = $"{playerName}: {LocalizationManager.Translate("classification.result", score)}";
	}

	private void OnMainMenuPressed()
	{
		GetTree().ChangeSceneToFile("res://Scenes/UI/MainMenu.tscn");
	}
}
