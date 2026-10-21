using OpenTK.Mathematics;

namespace Spacebox.Game.Player.Footsteps;

public sealed class PlayerFootsteps
{
    public const float LandingMinSpeed = 2f;
    public const float LandingVolume = 0.9f;

    private readonly GravityWalker _walker;
    private readonly FootstepCadence _cadence = new FootstepCadence();

    public PlayerFootsteps(GravityWalker walker)
    {
        _walker = walker;
        _walker.Landed += OnLanded;
    }

    public void Update(float deltaTime)
    {
        if (!_walker.IsActive || !_walker.IsGrounded)
        {
            _cadence.Reset();
            return;
        }

        var gait = _walker.Gait;
        if (!_cadence.Advance(_walker.HorizontalSpeed, deltaTime, gait)) return;
        if (_walker.TryGetGroundBlock(out var block)) FootstepSounds.Play(FootstepSurfaces.For(block), FootstepSounds.Volume(gait));
    }

    private void OnLanded(float impactSpeed, Vector3 gravity)
    {
        if (impactSpeed < LandingMinSpeed || !_walker.TryGetGroundBlock(out var block)) return;
        FootstepSounds.Play(FootstepSurfaces.For(block), LandingVolume);
    }
}
