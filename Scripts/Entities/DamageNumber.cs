using Godot;

public partial class DamageNumber : Node2D
{
    [Export] public float Duration = 0.8f;
    [Export] public float FloatSpeed = 60f;

    private float _timer = 0f;
    private Label _label;

    public void Initialize(float damage, bool isCrit = false)
    {
        ZIndex = 10; //Esto hace que salga siempre encima de todo
        _label = GetNode<Label>("Label");
        _label.Text = Mathf.Round(damage).ToString();

        //Los crits son más grandes y de color distinto (preparado para futuro)
        if (isCrit)
        {
            _label.AddThemeFontSizeOverride("font_size", 24);
            _label.AddThemeColorOverride("font_color", new Color(1f, 0.8f, 0f));
        }
        else
        {
            _label.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
        }
    }

    public void InitializeReaction(string reactionName, float reactionDamage)
    {
        ZIndex = 10;
        _label = GetNode<Label>("Label");
        _label.Text = reactionDamage >= 1f
            ? $"{reactionName}\n{Mathf.Round(reactionDamage)}"
            : reactionName;
        _label.Position = new Vector2(-65f, -12f);
        _label.Size = new Vector2(130f, 48f);
        _label.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
    }

    public override void _Process(double delta)
    {
        _timer += (float)delta;

        //El texto flota hacia arriba
        Position += Vector2.Up * FloatSpeed * (float)delta;

        //El texto se desvanece gradualmente
        float alpha = 1f - (_timer / Duration);
        Modulate = new Color(1, 1, 1, Mathf.Max(0, alpha));

        if (_timer >= Duration)
            QueueFree();
    }
}
