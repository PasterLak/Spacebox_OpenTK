using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Mobs;
using Spacebox.Game.Player;

namespace Spacebox.Game.Radar;

public sealed class RadarScanner
{
    public const float Range = 500f;

    private readonly List<RadarContact> _visible = new List<RadarContact>();
    private readonly List<RadarContact> _blips = new List<RadarContact>();

    public IReadOnlyList<RadarContact> Blips => _blips;

    public bool Sweep(Astronaut viewer, float previousAngle, float angle, float fadePerSecond, float deltaTime)
    {
        Fade(fadePerSecond * deltaTime);
        if (viewer == null) return false;

        Collect(viewer);
        bool found = false;
        for (int i = 0; i < _visible.Count; i++)
        {
            var contact = _visible[i];
            if (!RadarMath.Crossed(previousAngle, angle, RadarMath.SweepAngleOf(contact.Direction))) continue;

            contact.Alpha = 1f;
            _blips.Add(contact);
            found = true;
        }
        return found;
    }

    public void Clear()
    {
        _visible.Clear();
        _blips.Clear();
    }

    private void Fade(float amount)
    {
        for (int i = _blips.Count - 1; i >= 0; i--)
        {
            var blip = _blips[i];
            blip.Alpha -= amount;
            if (blip.Alpha <= 0f) _blips.RemoveAt(i);
            else _blips[i] = blip;
        }
    }

    private void Collect(Astronaut viewer)
    {
        _visible.Clear();
        var origin = viewer.Position;
        var right = viewer.Right;
        var front = viewer.Front;

        var entities = World.CurrentSector?.Entities;
        if (entities != null)
        {
            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                if (entity == null || entity.IsDisposed || entity.Ship == null) continue;
                Add(RadarContactKind.Ship, entity.CenterOfMassWorld - origin, right, front);
            }
        }

        var bodies = World.Instance?.Physics?.Bodies;
        if (bodies == null) return;
        for (int i = 0; i < bodies.Count; i++)
        {
            switch (bodies[i].Owner)
            {
                case RemoteAstronaut remote when remote.IsAliveVisual:
                    Add(RadarContactKind.Player, remote.Position - origin, right, front);
                    break;
                case Mob mob:
                    Add(RadarContactKind.Mob, mob.Position - origin, right, front);
                    break;
            }
        }
    }

    private void Add(RadarContactKind kind, Vector3 offset, Vector3 right, Vector3 front)
    {
        float distance = offset.Length;
        if (distance > Range || distance < 1f) return;

        _visible.Add(new RadarContact { Kind = kind, Direction = RadarMath.Project(offset, right, front), Distance = distance });
    }
}
