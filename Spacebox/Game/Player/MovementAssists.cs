namespace Spacebox.Game.Player;

public static class MovementAssists
{
    public static bool IsSprinting(bool sprintHeld, bool autoSprint, bool forwardHeld) => sprintHeld || (autoSprint && forwardHeld);

    public static bool ShouldAutoJump(bool enabled, bool grounded, bool moving, bool crouching, bool blockedAhead, bool clearAboveStep)
    {
        return enabled && grounded && moving && !crouching && blockedAhead && clearAboveStep;
    }

    public static bool WantsSprint() => IsSprinting(Engine.Input.IsAction("sprint"), Settings.Gameplay.AutoSprint, Engine.Input.IsAction("forward"));
}
