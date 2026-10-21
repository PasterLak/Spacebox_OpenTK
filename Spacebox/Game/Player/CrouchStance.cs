namespace Spacebox.Game.Player;

public sealed class CrouchStance
{
    public const float TransitionSpeed = 5f;
    private const float Epsilon = 1e-3f;

    public CrouchStance(float standingEyeHeight, float crouchingEyeHeight)
    {
        StandingEyeHeight = standingEyeHeight;
        CrouchingEyeHeight = crouchingEyeHeight;
        EyeHeight = standingEyeHeight;
    }

    public float StandingEyeHeight { get; }
    public float CrouchingEyeHeight { get; }
    public float EyeHeight { get; private set; }
    public bool IsCrouching => EyeHeight < StandingEyeHeight - Epsilon;

    public void Reset(float eyeHeight)
    {
        EyeHeight = Math.Clamp(eyeHeight, CrouchingEyeHeight, StandingEyeHeight);
    }

    public float Update(bool wantsCrouch, float deltaTime, Func<float, bool> fits)
    {
        float target = wantsCrouch ? CrouchingEyeHeight : StandingEyeHeight;
        if (MathF.Abs(target - EyeHeight) < Epsilon) return EyeHeight;

        float step = TransitionSpeed * deltaTime;
        float next = target < EyeHeight ? MathF.Max(target, EyeHeight - step) : MathF.Min(target, EyeHeight + step);
        if (next > EyeHeight && !fits(next)) return EyeHeight;

        EyeHeight = next;
        return next;
    }
}
