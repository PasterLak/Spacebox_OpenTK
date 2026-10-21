using Spacebox.Game.Effects;

namespace Spacebox.Game.Player.Footsteps;

public static class FootstepSounds
{
    public const float PitchVariation = 0.06f;

    private static readonly string[] StoneNames = { "footstepStone1", "footstepStone2", "footstepStone3", "footstepStone4" };
    private static readonly string[] MetalNames = { "footstepMetal1", "footstepMetal2", "footstepMetal3", "footstepMetal4" };
    private static readonly LocalSoundPlayer _player = new LocalSoundPlayer();
    private static readonly Random _random = new Random();
    private static int _lastVariant = -1;

    public static float Volume(FootstepGait gait) => gait switch
    {
        FootstepGait.Crouch => 0.3f,
        FootstepGait.Run => 0.8f,
        _ => 0.55f
    };

    public static void Play(FootstepSurface surface, float volume)
    {
        var names = surface == FootstepSurface.Stone ? StoneNames : MetalNames;
        int variant = _random.Next(names.Length - 1);
        if (variant >= _lastVariant && _lastVariant >= 0) variant++;
        _lastVariant = variant;

        float pitch = 1f + ((float)_random.NextDouble() * 2f - 1f) * PitchVariation;
        _player.Play(names[variant], volume, pitch);
    }

    public static void Clear()
    {
        _player.Clear();
        _lastVariant = -1;
    }
}
