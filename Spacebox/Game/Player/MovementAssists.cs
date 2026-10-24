namespace Spacebox.Game.Player;

public static class MovementAssists
{
    private static readonly SprintToggle _toggle = new SprintToggle();
    private static float _lastUpdate = -1f;
    private static bool _lastResult;

    public static bool ShouldAutoJump(bool enabled, bool grounded, bool moving, bool crouching, bool blockedAhead, bool clearAboveStep)
    {
        return enabled && grounded && moving && !crouching && blockedAhead && clearAboveStep;
    }

    public static bool WantsSprint(bool moving)
    {
        float now = Engine.Time.Total;
        if (now == _lastUpdate) return _lastResult;

        _lastUpdate = now;
        _lastResult = _toggle.Update(Settings.Gameplay.AutoSprint, Engine.Input.IsActionDown("sprint"), Engine.Input.IsAction("sprint"), moving);
        return _lastResult;
    }

    public static void Reset()
    {
        _toggle.Reset();
        _lastUpdate = -1f;
        _lastResult = false;
    }
}

public sealed class SprintToggle
{
    public bool IsOn { get; private set; }

    public bool Update(bool toggleMode, bool pressed, bool held, bool moving)
    {
        if (!toggleMode)
        {
            IsOn = false;
            return held;
        }

        if (pressed) IsOn = !IsOn;
        if (!moving) IsOn = false;
        return IsOn;
    }

    public void Reset() => IsOn = false;
}
