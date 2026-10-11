using System.Runtime.CompilerServices;
using Godot;

public partial class UpgradeScreen : Control
{
    [Export] public PackedScene UpgradeCardScene { get; set; }

    private Player _player;

    public override void _Ready()
    {
        Visible = false;
        _player = GetTree().GetFirstNodeInGroup("player") as Player;
    }

    public void ShowCard(System.Collections.Generic.List<Upgrade> choices)
    {
        Visible = true;
        GetTree().Paused = true;
        AudioManager.Instance?.SetAudioPaused(true);

        //Se limpia las cartas anteriores si es que las hubiera
        var container = GetNode<HBoxContainer>("CardsContainer");
        foreach (Node child in container.GetChildren())
            child.QueueFree();
        
        //Se crea una carta por cada opción
        foreach (var upgrade in choices)
        {
            UpgradeCard card = UpgradeCardScene.Instantiate<UpgradeCard>();
            container.AddChild(card);
            card.Initialize(upgrade);

            //Cuando el jugador elige una carta, se aplica el upgrade
            card.OnUpgradeSelected += OnUpgradeChosen;
        }
    }

    private void OnUpgradeChosen(Upgrade upgrade)
    {
        GetNodeOrNull<Node>("/root/ClassificationManager")?.Call("AddCardScore");
        _player.Upgrades.ApplyUpgrade(upgrade);

        Visible = false;
        GetTree().Paused = false;
        AudioManager.Instance?.SetAudioPaused(false);
    }
}
