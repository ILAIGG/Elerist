using Godot;

public partial class MainMenu : Control
{
    private Button _playButton;
    private Button _continueButton;
    private Button _endlessModeButton;
    private Button _settingsButton;
    private Button _quitButton;
    private Button _classificationButton;
    private Label _titleLabel;
    private Label _languageLabel;
    private Label _versionLabel;
    private OptionButton _languageOption;
    private PackedScene _confirmDialog = GD.Load<PackedScene>("res://Scenes/UI/ConfirmDialog.tscn");

    public override void _Ready()
    {
        GameManager.Instance?.ClearActiveSave();
        SettingsMenu.LoadSavedControls();
        _playButton = GetNode<Button>("PlayButton");
        _continueButton = GetNode<Button>("ContinueButton");
        _endlessModeButton = GetNode<Button>("EndlessModeButton");
        _settingsButton = GetNode<Button>("SettingsButton");
        _quitButton = GetNode<Button>("QuitButton");
        _classificationButton = GetNodeOrNull<Button>("ClassificationButton");
        _titleLabel = GetNode<Label>("Title");
        _languageLabel = GetNode<Label>("LanguageLabel");
        _versionLabel = GetNode<Label>("VersionLabel");
        _languageOption = GetNode<OptionButton>("LanguageOption");

        _playButton.Pressed += OnPlayPressed;
        _continueButton.Pressed += OnContinuePressed;
        _settingsButton.Pressed += OnSettingsPressed;
        _quitButton.Pressed += OnQuitPressed;
        if (_classificationButton != null)
            _classificationButton.Pressed += OnClassificationPressed;
        _languageOption.ItemSelected += OnLanguageSelected;

        PopulateLanguages();
        RefreshLocalizedText();
        SaveData savedRun = SaveSystem.LoadRun();
        _continueButton.Disabled = savedRun == null || savedRun.IsRunComplete;

        //TESTS BORRAR LUEGO!!!!!!
        AudioManager.Instance.PlayMusic("r!ckes-desert.theme");
    }

    private void PopulateLanguages()
    {
        _languageOption.Clear();
        _languageOption.AddItem(LocalizationManager.Translate("language.english"));
        _languageOption.AddItem(LocalizationManager.Translate("language.spanish"));
        _languageOption.Selected = LocalizationManager.Instance.CurrentLocale == LocalizationManager.SpanishLocale ? 1 : 0;
    }

    private void OnLanguageSelected(long index)
    {
        LocalizationManager.Instance.SetLocale(index == 1 ? LocalizationManager.SpanishLocale : LocalizationManager.DefaultLocale);
        PopulateLanguages();
        RefreshLocalizedText();
    }

    private void RefreshLocalizedText()
    {
        _titleLabel.Text = LocalizationManager.Translate("menu.title");
        _playButton.Text = LocalizationManager.Translate("menu.new_game");
        _continueButton.Text = LocalizationManager.Translate("menu.continue");
        _settingsButton.Text = LocalizationManager.Translate("menu.settings");
        _quitButton.Text = LocalizationManager.Translate("menu.quit");
        if (_classificationButton != null)
            _classificationButton.Text = LocalizationManager.Translate("menu.classification");
        _languageLabel.Text = LocalizationManager.Translate("menu.language");
        _versionLabel.Text = LocalizationManager.Translate("menu.version");
    }

    private void OnPlayPressed()
    {
        if (ResourceLoader.Exists("res://Scenes/UI/PlayerNameDialog.tscn"))
        {
            PlayerNameDialog nameDialog = GD.Load<PackedScene>("res://Scenes/UI/PlayerNameDialog.tscn").Instantiate<PlayerNameDialog>();
            AddChild(nameDialog);
            nameDialog.Confirmed += ShowStartConfirmation;
            return;
        }

        ShowStartConfirmation(LocalizationManager.Translate("classification.default_name"));
    }

    private void ShowStartConfirmation(string playerName)
    {
        ConfirmDialog dialog = _confirmDialog.Instantiate<ConfirmDialog>();
        AddChild(dialog);
        dialog.SetMessage(LocalizationManager.Translate("confirm.start_run"));

        dialog.OnConfirmed += () =>
        {
            GameManager.Instance.NewGame("Run");
            GetNodeOrNull<Node>("/root/ClassificationManager")?.Call("BeginRun", playerName);
            GetTree().ChangeSceneToFile("res://Scenes/World/NodeMap.tscn");
        };
    }

    private void OnContinuePressed()
    {
        ConfirmDialog dialog = _confirmDialog.Instantiate<ConfirmDialog>();
        AddChild(dialog);
        dialog.SetMessage(LocalizationManager.Translate("confirm.continue_run"));

        dialog.OnConfirmed += () =>
        {
            GameManager.Instance.LoadGame();
            GetNodeOrNull<Node>("/root/ClassificationManager")?.Call("BeginRun", GameManager.Instance.ActiveSave?.Name ?? LocalizationManager.Translate("classification.default_name"));
            GetTree().ChangeSceneToFile("res://Scenes/World/NodeMap.tscn");
        };
    }

    private void OnClassificationPressed()
    {
        const string scenePath = "res://Scenes/UI/Classification.tscn";
        if (ResourceLoader.Exists(scenePath))
            GetTree().ChangeSceneToFile(scenePath);
    }

    private void OnQuitPressed()
    {
        GetTree().Quit();
    }

    private void OnSettingsPressed()
    {
        AddChild(GD.Load<PackedScene>("res://Scenes/UI/SettingsMenu.tscn").Instantiate());
    }

#if DEBUG
    private Key[] _konamiCode = { Key.Up, Key.Up, Key.Down, Key.Down, Key.Left, Key.Right, Key.Left, Key.Right, Key.B, Key.A };
    private int _konamiIndex = 0;

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            if (keyEvent.Keycode == _konamiCode[_konamiIndex])
            {
                _konamiIndex++;
                if (_konamiIndex >= _konamiCode.Length)
                {
                    GameManager.Instance.GodModeEnabled = !GameManager.Instance.GodModeEnabled;
                    GD.Print("God Mode " + (GameManager.Instance.GodModeEnabled ? "Activated" : "Deactivated"));
                    _konamiIndex = 0;
                }
            }
            else
            {
                _konamiIndex = 0;
                if (keyEvent.Keycode == _konamiCode[0])
                    _konamiIndex = 1;
            }
        }
    }
#endif
}