using Engine;
using Engine.PostProcessing.CameraFeed;
using Spacebox.Game.Ships;

namespace Spacebox.Game.Rendering;

public static class CameraFeedController
{
    public static bool IsActive { get; private set; }
    public static float ActiveSeconds { get; private set; }

    public static void Update()
    {
        bool active = ShipPilot.IsUsingCamera;
        if (active && !IsActive) ActiveSeconds = 0f;
        if (active) ActiveSeconds += Time.Delta;

        IsActive = active;
        var effect = GetEffect();
        if (effect != null) effect.Active = active;
    }

    public static void Reset()
    {
        IsActive = false;
        ActiveSeconds = 0f;
        var effect = GetEffect();
        if (effect != null) effect.Active = false;
    }

    public static bool IsVisible => IsActive && GetEffect() is { Intensity: > 0f };

    private static CameraFeedEffect GetEffect()
    {
        return (EngineWindow.Instance as SpaceboxWindow)?.CameraFeed;
    }
}
