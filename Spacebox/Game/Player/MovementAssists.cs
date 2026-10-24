namespace Spacebox.Game.Player;

public static class MovementAssists
{
    public static bool ShouldAutoJump(bool enabled, bool grounded, bool moving, bool crouching, bool blockedAhead, bool clearAboveStep)
    {
        return enabled && grounded && moving && !crouching && blockedAhead && clearAboveStep;
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
