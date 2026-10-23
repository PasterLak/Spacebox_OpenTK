namespace Spacebox.Game.Player.Interactions;

public sealed class DrillSoundMixer
{
    public const float BaseVolume = 0.35f;
    public const float FadeInSeconds = 0.2f;
    public const float FadeOutSeconds = 0.35f;
    public const float PitchSmoothing = 5f;
    public const float FreeSpinPitch = 1.06f;
    public const float FreeSpinVolume = 0.6f;
    public const float TargetPitch = 0.98f;
    public const float BlockedPitch = 0.82f;
    public const float BlockedVolume = 0.7f;
    public const float SoftestPitch = 1.04f;
    public const float PitchPerHardness = 0.04f;
    public const int MaxHardnessSteps = 4;
    public const float SilenceThreshold = 0.0005f;

    private bool _hasTarget;
    private float _targetPitch = 1f;
    private float _targetVolume;

    public float Volume { get; private set; }
    public float Pitch { get; private set; } = 1f;
    public bool IsAudible => Volume > SilenceThreshold;

    public static float PitchForHardness(int hardness)
    {
        int steps = Math.Clamp(hardness - 1, 0, MaxHardnessSteps);
        return SoftestPitch - steps * PitchPerHardness;
    }

    public void DrillBlock(int hardness, bool canDrill)
    {
        if (canDrill) Target(PitchForHardness(hardness), 1f);
        else Target(BlockedPitch, BlockedVolume);
    }

    public void DrillTarget() => Target(TargetPitch, 1f);

    public void SpinFree() => Target(FreeSpinPitch, FreeSpinVolume);

    public void Update(float deltaTime)
    {
        float dt = Math.Max(0f, deltaTime);
        float target = _hasTarget ? BaseVolume * _targetVolume : 0f;
        float fade = target > Volume ? FadeInSeconds : FadeOutSeconds;
        float step = BaseVolume / Math.Max(0.001f, fade) * dt;
        Volume = MoveTowards(Volume, target, step);

        if (_hasTarget)
        {
            if (Volume <= step) Pitch = _targetPitch;
            else Pitch += (_targetPitch - Pitch) * (1f - MathF.Exp(-dt * PitchSmoothing));
        }

        _hasTarget = false;
    }

    public void Silence()
    {
        _hasTarget = false;
        Volume = 0f;
    }

    private void Target(float pitch, float volume)
    {
        _hasTarget = true;
        _targetPitch = pitch;
        _targetVolume = volume;
    }

    private static float MoveTowards(float value, float target, float step)
    {
        if (value < target) return Math.Min(target, value + step);
        return Math.Max(target, value - step);
    }
}
