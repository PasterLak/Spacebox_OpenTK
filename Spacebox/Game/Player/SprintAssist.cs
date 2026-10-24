namespace Spacebox.Game.Player;

public sealed class SprintAssist
{
    private readonly SprintToggle _toggle = new SprintToggle();
    private float _lastUpdate = -1f;
    private bool _lastResult;

    public bool WantsSprint(bool moving)
    {
        float now = Engine.Time.Total;
        if (now == _lastUpdate) return _lastResult;

        _lastUpdate = now;
        _lastResult = _toggle.Update(Settings.Gameplay.AutoSprint, Engine.Input.IsActionDown("sprint"), Engine.Input.IsAction("sprint"), moving);
        return _lastResult;
    }
}
