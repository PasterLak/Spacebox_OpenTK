using Spacebox.Game.GUI.Sounds;

namespace Spacebox.Game.Player;

public static class LowOxygenAlarm
{
    private static readonly LowOxygenBeepSchedule _schedule = new LowOxygenBeepSchedule();

    public static void Update(LocalAstronaut player, float deltaTime, bool suspended)
    {
        float level = suspended ? 0f : OxygenWarning.For(player);
        if (_schedule.Update(level, deltaTime)) UiSounds.Play(UiSound.LowOxygen);
    }

    public static void Reset()
    {
        _schedule.Reset();
    }
}
