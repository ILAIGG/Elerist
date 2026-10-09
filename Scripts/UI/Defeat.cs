using Godot;

public partial class Defeat : Control
{
    private Button _retryButton;
    private Button _returnToMapButton;
    private Button _mainMenuButton;

    public override void _Ready()
    {
        Visible = false;

        _retryButton = GetNodeOrNull<Button>("UIContainer/Panel/Margin/Content/RetryButton");
        _returnToMapButton = GetNodeOrNull<Button>("UIContainer/Panel/Margin/Content/ReturnToMapButton");
        _mainMenuButton = GetNodeOrNull<Button>("UIContainer/Panel/Margin/Content/MainMenuButton");

        Label titleLabel = GetNodeOrNull<Label>("UIContainer/Title");
        if (titleLabel != null)
            titleLabel.Text = LocalizationManager.Translate("screen.defeat");

        if (_retryButton != null)
            _retryButton.Text = LocalizationManager.Translate("common.retry");
        if (_returnToMapButton != null)
            _returnToMapButton.Text = LocalizationManager.Translate("common.return_to_map");
        if (_mainMenuButton != null)
            _mainMenuButton.Text = LocalizationManager.Translate("common.main_menu");

        if (_retryButton != null)
            _retryButton.Pressed += OnRetryPressed;
        if (_returnToMapButton != null)
            _returnToMapButton.Pressed += OnReturnToMapPressed;
        if (_mainMenuButton != null)
            _mainMenuButton.Pressed += OnMainMenuPressed;
    }

    public void ShowDefeat()
    {
        Visible = true;
        GetTree().Paused = true;
    }

    private void OnRetryPressed()
    {
        GetTree().Paused = false;
        GetTree().ReloadCurrentScene();
    }

    private void OnReturnToMapPressed()
    {
        GetTree().Paused = false;
        GetTree().ChangeSceneToFile("res://Scenes/World/NodeMap.tscn");
    }

    private void OnMainMenuPressed()
    {
        GetTree().Paused = false;
        GetTree().ChangeSceneToFile("res://Scenes/UI/MainMenu.tscn");
    }
}