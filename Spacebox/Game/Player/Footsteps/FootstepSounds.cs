using Spacebox.Game.Effects;

namespace Spacebox.Game.Player.Footsteps;

public static class FootstepSounds
{
    public const float PitchVariation = 0.06f;

    private static readonly LocalSoundPlayer _player = new LocalSoundPlayer();
    private static readonly Random _random = new Random();
    private static int _lastVariant = -1;

    public static float Volume(FootstepGait gait) => gait switch
    {
        FootstepGait.Crouch => 0.25f,
        FootstepGait.Run => 0.6f,
        _ => 0.42f
    };

    public static void Play(string[] names, float volume)
    {
        if (names == null || names.Length == 0) return;

        int variant = names.Length == 1 ? 0 : _random.Next(names.Length - 1);
        if (names.Length > 1 && variant >= _lastVariant && _lastVariant >= 0) variant++;
        if (variant >= names.Length) variant = 0;
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
