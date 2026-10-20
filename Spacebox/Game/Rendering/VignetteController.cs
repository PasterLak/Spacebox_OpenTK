using Engine;
using Engine.PostProcessing;

namespace Spacebox.Game.Rendering;

public readonly record struct VignetteState(float Strength, float Radius, bool Enabled, bool AlwaysApply);

public static class VignetteController
{
    public const float DefaultStrength = 0.5f;
    public const float BaseRadius = 0.75f;
    public const float WarningRadius = 0.55f;
    public const float WarningStrengthScale = 1.5f;

    private static float _settingStrength = DefaultStrength;
    private static float _warning;

    public static void SetSettingStrength(float strength)
    {
        _settingStrength = Math.Clamp(strength, 0f, 1f);
        Apply();
    }

    public static void SetWarning(float warning)
    {
        float clamped = Math.Clamp(warning, 0f, 1f);
        if (clamped == _warning) return;
        _warning = clamped;
        Apply();
    }

    public static VignetteState Compute(float settingStrength, float warning)
    {
        float target = MathF.Max(settingStrength, DefaultStrength) * WarningStrengthScale;
        float strength = settingStrength + (target - settingStrength) * warning;
        float radius = BaseRadius + (WarningRadius - BaseRadius) * warning;
        return new VignetteState(strength, radius, strength > 0f, warning > 0f);
    }

    private static void Apply()
    {
        var effect = (EngineWindow.Instance as SpaceboxWindow)?.Vignette;
        if (effect == null) return;

        var state = Compute(_settingStrength, _warning);
        effect.VignetteStrength = state.Strength;
        effect.VignetteRadius = state.Radius;
        effect.Enabled = state.Enabled;
        effect.AlwaysApply = state.AlwaysApply;
    }
}
