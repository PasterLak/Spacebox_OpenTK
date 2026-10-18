using Engine;
using Engine.Audio;
using Engine.Physics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Ships;

public static class ShipWeapons
{
    private const int SoundSources = 6;

    private static readonly AudioSource[] _sounds = new AudioSource[SoundSources];
    private static int _nextSound;
    private static bool _registered;

    public static event Action<ShipShot> ShotFired;

    public static void Register()
    {
        if (_registered) return;
        _registered = true;
        ShipControlSystem.CannonFired += OnCannonFired;
    }

    public static void Reset()
    {
        for (int i = 0; i < _sounds.Length; i++)
        {
            _sounds[i]?.Dispose();
            _sounds[i] = null;
        }
    }

    public static void Spawn(in ShipShot shot, bool authoritative, float age)
    {
        var world = World.Instance;
        if (world == null || GameAssets.GetBlockDataById(shot.CannonBlockId) is not CannonBlockData data) return;
        if (!GameAssets.TryGetProjectileByName(data.ProjectileName, out var parameters)) return;

        world.TryFindEntity(shot.EntityId, out var entity);

        var projectile = world.ProjectilesPool.Take();
        projectile.Initialize(new Ray(shot.Origin, shot.Direction, 1f), ref parameters, null, shot.ShooterPeer, authoritative, 0f);
        projectile.SetCarrier(shot.CarrierVelocity, entity, ShipPhysicsSettings.Current.ProjectileOwnerIgnoreTime);
        if (age > 0f) projectile.FastForward(age);
        ProjectileImpactEffects.Attach(projectile);

        PlaySound(data.ShotSound, shot.Origin);
    }

    private static void OnCannonFired(ShipControlSystem control, CannonBlock cannon)
    {
        var entity = control.Entity;
        var origin = entity.EntityLocalToWorld(cannon.MuzzleLocal);

        var shot = new ShipShot
        {
            CannonBlockId = cannon.Id,
            EntityId = entity.EntityID,
            ShooterPeer = cannon.ShooterPeer,
            Origin = origin,
            Direction = entity.DirectionToWorldSpace(cannon.AimDirection).Normalized(),
            CarrierVelocity = entity.Ship?.GetPointVelocity(origin) ?? OpenTK.Mathematics.Vector3.Zero
        };

        Spawn(shot, true, 0f);
        ShotFired?.Invoke(shot);
    }

    private static void PlaySound(string soundName, OpenTK.Mathematics.Vector3 position)
    {
        if (Spacebox.Network.GameSession.IsDedicated || string.IsNullOrEmpty(soundName)) return;
        if (!GameAssets.Sounds.TryGetValue(soundName, out var clip)) return;

        var source = _sounds[_nextSound];
        if (source == null || source.IsDisposed || source.Clip != clip)
        {
            source?.Dispose();
            source = new AudioSource(clip);
            source.Setup3D(10f, 400f, 1.2f);
            _sounds[_nextSound] = source;
        }

        _nextSound = (_nextSound + 1) % _sounds.Length;
        source.Position = position;
        source.Play();
    }
}
