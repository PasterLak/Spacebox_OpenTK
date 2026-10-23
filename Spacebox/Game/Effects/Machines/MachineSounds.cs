using Engine;
using Spacebox.Game.Generation;

namespace Spacebox.Game.Effects.Machines;

public static class MachineSounds
{
    public const float HearingRadius = 16f;
    public const float RefreshInterval = 0.25f;

    private static readonly MachineSoundTracker _tracker = new MachineSoundTracker(MachineActivities.CreateDefault(), HearingRadius);
    private static readonly MachineLoopVoices _voices = new MachineLoopVoices(HearingRadius);
    private static float _refreshTimer;

    public static void Update(float deltaTime)
    {
        if (Application.IsHeadless) return;

        _refreshTimer -= deltaTime;
        if (_refreshTimer <= 0f)
        {
            _refreshTimer = RefreshInterval;
            var listener = Camera.Main?.PositionWorld;
            if (listener.HasValue) _tracker.Refresh(World.CurrentSector, listener.Value);
            else _tracker.Clear();
            _voices.Assign(_tracker.Working);
        }

        _voices.Update(deltaTime);
    }

    public static void Clear()
    {
        _voices.Clear();
        _tracker.Clear();
        _refreshTimer = 0f;
    }
}
