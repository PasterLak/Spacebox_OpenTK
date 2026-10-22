using Engine.Audio;
using OpenTK.Mathematics;
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
    private readonly Dictionary<string, AudioSource> _sounds = new Dictionary<string, AudioSource>();
    private float _range = 50f;

    public bool HasEngines => _engines.Count > 0;
    public bool HasCannons => _cannons.Count > 0;

    public void SetParts(ShipViewerParts parts, Matrix4 view, float radius)
    {
        _engines.Clear();
        _cannons.Clear();
        _range = radius * RangeInRadii;

        AddEngines(parts.Thrusters, view, true);
        if (_engines.Count == 0) AddEngines(parts.Thrusters, view, false);

        for (int i = 0; i < parts.Cannons.Count; i++)
        {
            var cannon = parts.Cannons[i];
            _cannons.Add(new ViewerCannon(Vector3.TransformPosition(cannon.Muzzle, view), Vector3.TransformVector(cannon.Direction, view), cannon.Data));
        }
    }

    public void Update(float deltaTime, bool enginesOn)
    {
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
        for (int i = 0; i < _cannons.Count; i++)
        {
            var cannon = _cannons[i];
            if (!GameAssets.TryGetProjectileByName(cannon.Data.ProjectileName, out var projectile)) continue;

            float range = projectile.MaxTravelDistance > 0 ? MathF.Min(projectile.MaxTravelDistance, _range) : _range;
            _shots.Fire(cannon.Muzzle, cannon.Direction, projectile, range);
            PlaySound(cannon.Data.ShotSound);
        }
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
        _sounds.Clear();
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
