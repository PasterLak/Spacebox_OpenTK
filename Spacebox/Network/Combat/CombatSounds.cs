using Engine.Audio;
using OpenTK.Mathematics;
using Spacebox.Game;
using Spacebox.Game.Effects;

namespace Spacebox.Network.Combat;

public sealed class CombatSounds
{
    private const int SourcesPerSound = 4;

    private readonly SpatialSoundPool _pool = new SpatialSoundPool(SourcesPerSound, 5f, 300f, 1.5f);
    private readonly Random _random = new Random();

    public void PlayShot(WeaponItem weapon, Vector3 position)
    {
        if (string.IsNullOrEmpty(weapon.ShotSound) || !GameAssets.Sounds.TryGetValue(weapon.ShotSound, out var clip)) return;

        _pool.Play(clip, position, _random.Next(95, 105) * 0.01f);
    }

    public void Clear()
    {
        _pool.Clear();
    }
}
