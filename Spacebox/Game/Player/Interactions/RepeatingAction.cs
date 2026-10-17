using Engine;

namespace Spacebox.Game.Player.Interactions;

public sealed class RepeatingAction
{
    private readonly string _action;
    private readonly float _interval;
    private float _time;

    public RepeatingAction(string action, float interval)
    {
        _action = action;
        _interval = interval;
        _time = interval;
    }

    public bool Poll()
    {
        if (Input.IsActionDown(_action))
        {
            _time = _interval;
            return true;
        }

        if (!Input.IsAction(_action))
        {
            _time = _interval;
            return false;
        }

        _time -= Time.Delta;
        if (_time > 0) return false;

        _time = _interval;
        return true;
    }
}
