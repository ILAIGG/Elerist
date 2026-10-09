using Godot;
using System;

public partial class UpgradeCard : PanelContainer
{
	//Evento que se dispara cuando el jugador clickea esta carta
	public event Action<Upgrade> OnUpgradeSelected;

	private bool _inventoryMode = false;

	private Upgrade _upgrade;

	public void Initialize(Upgrade upgrade)
	{
		_inventoryMode = false;
		_upgrade = upgrade;

		string localizedName = LocalizationManager.Translate(upgrade.NameKey);
		GetNode<Label>("CardContent/UpgradeName").Text = localizedName;
		GetNode<Label>("CardContent/UpgradeType").Text = LocalizationManager.Translate($"upgrade.type.{upgrade.Type.ToString().ToLowerInvariant()}");
		SetElementLabel(upgrade.ElementType);

		int nextApplication = upgrade.TimesApplied + 1;
		string descriptionKey = upgrade.GetLocalizedDescriptionKey(nextApplication);
		string localizedDescription = LocalizationManager.Translate(
			descriptionKey,
			upgrade.GetLocalizedDescriptionArguments(nextApplication));
		GetNode<Label>("CardContent/UpgradeDescription").Text = localizedDescription;
	}

	public void InitializeInventory(Upgrade upgrade)
	{
		_inventoryMode = true;
		_upgrade = upgrade;

		string localizedName = LocalizationManager.Translate(upgrade.NameKey);
		GetNode<Label>("CardContent/UpgradeName").Text = localizedName;

		GetNode<Label>("CardContent/UpgradeType").Text =
			LocalizationManager.Translate(
				$"upgrade.type.{upgrade.Type.ToString().ToLowerInvariant()}");

		SetElementLabel(upgrade.ElementType);

		int currentApplication = upgrade.TimesApplied;

		string descriptionKey =
			upgrade.GetLocalizedDescriptionKey(currentApplication);

		string localizedDescription =
			LocalizationManager.Translate(
				descriptionKey,
				upgrade.GetLocalizedDescriptionArguments(currentApplication));

		GetNode<Label>("CardContent/UpgradeDescription").Text =
			localizedDescription;
	}

	private void SetElementLabel(Element element)
	{
		Label elementLabel = GetNode<Label>("CardContent/UpgradeElement");
		elementLabel.Text = LocalizationManager.Translate(
			$"upgrade.element.{element.ToString().ToLowerInvariant()}");
		elementLabel.AddThemeColorOverride("font_color", ElementColorPalette.GetColor(element));
	}

    public override void _GuiInput(InputEvent @event)
    {
		if (_inventoryMode)
        	return;

        //Detecta cuando el jugador clickea la carta
		if (@event is InputEventMouseButton mb && mb.Pressed)
		{
			AudioManager.Instance?.PlaySfx("ui.click", true);
			OnUpgradeSelected?.Invoke(_upgrade);
		}
    }
}
