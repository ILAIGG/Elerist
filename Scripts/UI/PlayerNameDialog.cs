using Godot;
using System;

public partial class PlayerNameDialog : Control
{
    private LineEdit _nameInput;

    [Signal]
    public delegate void ConfirmedEventHandler(string playerName);

    public override void _Ready()
    {
        _nameInput = GetNode<LineEdit>("Panel/Margin/Content/NameInput");
        GetNode<Label>("Panel/Margin/Content/Title").Text = LocalizationManager.Translate("classification.player_name");
        _nameInput.PlaceholderText = LocalizationManager.Translate("classification.name_placeholder");
        GetNode<Button>("Panel/Margin/Content/Buttons/CancelButton").Text = LocalizationManager.Translate("common.cancel");
        GetNode<Button>("Panel/Margin/Content/Buttons/ConfirmButton").Text = LocalizationManager.Translate("common.confirm");
        GetNode<Button>("Panel/Margin/Content/Buttons/CancelButton").Pressed += QueueFree;
        GetNode<Button>("Panel/Margin/Content/Buttons/ConfirmButton").Pressed += Confirm;
        _nameInput.TextSubmitted += _ => Confirm();
        _nameInput.GrabFocus();
    }

    private void Confirm()
    {
        string playerName = _nameInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(playerName))
            playerName = LocalizationManager.Translate("classification.default_name");

        EmitSignal(SignalName.Confirmed, playerName);
        QueueFree();
    }
}