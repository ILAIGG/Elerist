using Godot;

public partial class SurviveTimeCondition : VictoryCondition
{
    [Export] public float TimeToSurvive { get; set; } = 120f; // 2 minutos por defecto

    private float _timer = 0f;
    private bool _completed = false;

    public override bool IsCompleted => _completed;

    public override void _Process(double delta)
    {
        if (_completed) return;

        _timer += (float)delta;

        if (_timer >= TimeToSurvive)
            _completed = true;
    }

    //Devuelve el tiempo restante en segundos
    public float GetTimeRemaining()
    {
        return Mathf.Max(0f, TimeToSurvive - _timer);
    }
}