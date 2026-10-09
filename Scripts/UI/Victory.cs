using Godot;

public partial class Victory : Control
{
    private Button _returnToMapButton;

    public override void _Ready()
    {
        Visible = false;

        _returnToMapButton = GetNodeOrNull<Button>("Content/VBoxContainer/ReturnToMapButton");
        Label titleLabel = GetNodeOrNull<Label>("Content/VBoxContainer/Title");

        if (titleLabel != null)
            titleLabel.Text = LocalizationManager.Translate("screen.victory");

        if (_returnToMapButton != null)
        {
            _returnToMapButton.Text = LocalizationManager.Translate("common.return_to_map");
            _returnToMapButton.Pressed += OnReturnToMapPressed;
        }
    }

    public void ShowVictory()
    {
        Visible = true;
        GetTree().Paused = true;
    }

    private void OnReturnToMapPressed()
    {
        GetTree().Paused = false;
        GetTree().ChangeSceneToFile("res://Scenes/World/NodeMap.tscn");
    }
}