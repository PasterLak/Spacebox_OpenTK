using Engine;
using Engine.Audio;
using OpenTK.Mathematics;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;
using Spacebox.Game.Ships;

namespace Spacebox.Game.ShipViewer;

public sealed class ShipViewerEffects : IDisposable
{
    private const float MainEngineAlignment = 0.9f;
    private const float RangeInRadii = 3f;

    private readonly ThrusterExhaustEmitter _exhaust = new ThrusterExhaustEmitter();
    private readonly CannonShotEmitter _shots = new CannonShotEmitter();
    private readonly List<ViewerThruster> _engines = new List<ViewerThruster>();
    private readonly List<ViewerCannon> _cannons = new List<ViewerCannon>();
    private readonly List<float> _reloads = new List<float>();
    private readonly Dictionary<string, AudioSource> _sounds = new Dictionary<string, AudioSource>();
    private AudioSource _hitSound;
    private float _range = 50f;

    public bool HasEngines => _engines.Count > 0;
    public bool HasCannons => _cannons.Count > 0;

    public void SetParts(ShipViewerParts parts, Matrix4 view, float radius)
    {
        _engines.Clear();
        _cannons.Clear();
        _reloads.Clear();
        _range = radius * RangeInRadii;

        AddEngines(parts.Thrusters, view, true);
        if (_engines.Count == 0) AddEngines(parts.Thrusters, view, false);

        for (int i = 0; i < parts.Cannons.Count; i++)
        {
            var cannon = parts.Cannons[i];
            _cannons.Add(new ViewerCannon(Vector3.TransformPosition(cannon.Muzzle, view), Vector3.TransformVector(cannon.Direction, view), cannon.Data));
            _reloads.Add(0f);
        }
    }

    public void Update(float deltaTime, bool enginesOn, bool autoFire)
    {
        if (autoFire) AutoFire(deltaTime);

        if (enginesOn)
        {
            for (int i = 0; i < _engines.Count; i++)
                _exhaust.Emit(_engines[i].Exhaust, _engines[i].Direction, Vector3.Zero, _engines[i].Nozzle, deltaTime);
        }

        _exhaust.Update();
        _shots.Update();
    }

    public void FireAll()
    {
        for (int i = 0; i < _cannons.Count; i++) Fire(i);
    }

    public void Shoot(Vector3 origin, Vector3 direction, float distance, in ProjectileParameters projectile)
    {
        _shots.Fire(origin, direction, projectile, distance);
    }

    public void Impact(Vector3 point, in ProjectileParameters projectile)
    {
        _shots.Impact(point, projectile);
        _hitSound ??= new AudioSource(Resources.Load<AudioClip>("hitBlock"), AudioChannel.UI);
        if (!_hitSound.IsDisposed) _hitSound.Play();
    }

    public void Render()
    {
        _exhaust.Render();
        _shots.Render();
    }

    public void Dispose()
    {
        _exhaust.Dispose();
        _shots.Dispose();
        foreach (var source in _sounds.Values) source.Dispose();
        _hitSound?.Dispose();
        _sounds.Clear();
    }

    private void AutoFire(float deltaTime)
    {
        for (int i = 0; i < _cannons.Count; i++)
        {
            _reloads[i] -= deltaTime;
            if (_reloads[i] <= 0f) Fire(i);
        }
    }

    private void Fire(int index)
    {
        var cannon = _cannons[index];
        _reloads[index] = cannon.Data.ReloadSeconds;
        if (!GameAssets.TryGetProjectileByName(cannon.Data.ProjectileName, out var projectile)) return;

        float range = projectile.MaxTravelDistance > 0 ? MathF.Min(projectile.MaxTravelDistance, _range) : _range;
        _shots.Fire(cannon.Muzzle, cannon.Direction, projectile, range);
        PlaySound(cannon.Data.ShotSound);
    }

    private void AddEngines(List<ViewerThruster> thrusters, Matrix4 view, bool mainOnly)
    {
        for (int i = 0; i < thrusters.Count; i++)
        {
            var thruster = thrusters[i];
            var direction = Vector3.TransformVector(thruster.Direction, view);
            if (mainOnly && Vector3.Dot(-direction, Vector3.UnitZ) < MainEngineAlignment) continue;
            _engines.Add(new ViewerThruster(Vector3.TransformPosition(thruster.Exhaust, view), direction, thruster.Nozzle));
        }
    }

    private void PlaySound(string name)
    {
        if (string.IsNullOrEmpty(name) || !GameAssets.Sounds.TryGetValue(name, out var clip)) return;

        if (!_sounds.TryGetValue(name, out var source) || source.IsDisposed)
        {
            source = new AudioSource(clip, AudioChannel.UI);
            _sounds[name] = source;
        }

        if (!source.IsPlaying) source.Play();
    }
}
