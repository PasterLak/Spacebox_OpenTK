using Engine;
using Spacebox.Game.Effects;

namespace Spacebox.Game.Player;

public static class AirZoneSounds
{
    public const string EnterSound = "airZoneEnter";
    public const string ExitSound = "airZoneExit";
    public const float Volume = 0.6f;
    public const double MinInterval = 0.5;

    private static readonly LocalSoundPlayer _player = new LocalSoundPlayer();
    private static double _lastPlayed = double.MinValue;

    public static void Play(bool enteredAir)
    {
        if (Time.Total - _lastPlayed < MinInterval) return;

        _lastPlayed = Time.Total;
        _player.Play(enteredAir ? EnterSound : ExitSound, Volume);
    }

    public static void Clear()
    {
        _player.Clear();
        _lastPlayed = double.MinValue;
    }
}
