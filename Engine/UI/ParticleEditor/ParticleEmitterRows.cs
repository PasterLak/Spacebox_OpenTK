using OpenTK.Mathematics;

namespace Engine.UI;

public static class ParticleEmitterRows
{
    private static readonly Dictionary<Type, Func<EmitterBase, ParticleRow[]>> _factories = new Dictionary<Type, Func<EmitterBase, ParticleRow[]>>();

    static ParticleEmitterRows()
    {
        Register<BoxEmitter>(b => new ParticleRow[]
        {
            Vector("Center", () => b.Center, v => b.Center = v),
            Vector("Size", () => b.Size, v => b.Size = v),
            Vector("Direction", () => b.Direction, v => b.Direction = v)
        });
        Register<ConeEmitter>(c => new ParticleRow[]
        {
            Vector("Apex", () => c.Apex, v => c.Apex = v),
            Vector("Axis", () => c.Axis, v => c.Axis = v),
            new ParticleNumberRow("Angle", () => c.Angle, v => c.Angle = v),
            new ParticleNumberRow("Height", () => c.Height, v => c.Height = v),
            Vector("Direction", () => c.Direction, v => c.Direction = v)
        });
        Register<DiskEmitter>(d => new ParticleRow[]
        {
            Vector("Center", () => d.Center, v => d.Center = v),
            Vector("Normal", () => d.Normal, v => d.Normal = v),
            new ParticleNumberRow("Radius", () => d.Radius, v => d.Radius = v)
        });
        Register<PlaneEmitter>(p => new ParticleRow[]
        {
            Vector("Center", () => p.Center, v => p.Center = v),
            Vector("Normal", () => p.Normal, v => p.Normal = v),
            new ParticleNumberRow("Width", () => p.Width, v => p.Width = v),
            new ParticleNumberRow("Height", () => p.Height, v => p.Height = v),
            Vector("Direction", () => p.Direction, v => p.Direction = v)
        });
        Register<SphereEmitter>(s => new ParticleRow[]
        {
            Vector("Center", () => s.Center, v => s.Center = v),
            new ParticleNumberRow("Radius", () => s.Radius, v => s.Radius = v)
        });
        Register<PointEmitter>(p => new ParticleRow[]
        {
            Vector("Position", () => p.Position, v => p.Position = v),
            Vector("Direction", () => p.Direction, v => p.Direction = v)
        });
    }

    public static void Register<T>(Func<T, ParticleRow[]> factory) where T : EmitterBase
    {
        _factories[typeof(T)] = emitter => factory((T)emitter);
    }

    public static ParticleRow[] For(EmitterBase emitter)
    {
        return emitter != null && _factories.TryGetValue(emitter.GetType(), out var factory) ? factory(emitter) : Array.Empty<ParticleRow>();
    }

    public static ParticleRow[] Common(EmitterBase e)
    {
        return new ParticleRow[]
        {
            new ParticleRangeRow("Speed", () => new Vector2(e.SpeedMin, e.SpeedMax), (a, b) => { e.SpeedMin = a; e.SpeedMax = b; }),
            new ParticleRangeRow("Life", () => new Vector2(e.LifeMin, e.LifeMax), (a, b) => { e.LifeMin = a; e.LifeMax = b; }),
            new ParticleRangeRow("Start Size", () => new Vector2(e.StartSizeMin, e.StartSizeMax), (a, b) => { e.StartSizeMin = a; e.StartSizeMax = b; }),
            new ParticleRangeRow("End Size", () => new Vector2(e.EndSizeMin, e.EndSizeMax), (a, b) => { e.EndSizeMin = a; e.EndSizeMax = b; }),
            Vector("Accel Start", () => e.AccelerationStart, v => e.AccelerationStart = v),
            Vector("Accel End", () => e.AccelerationEnd, v => e.AccelerationEnd = v),
            new ParticleRangeRow("Rot Speed", () => new Vector2(e.RotationSpeedMin, e.RotationSpeedMax), (a, b) => { e.RotationSpeedMin = a; e.RotationSpeedMax = b; }),
            new ParticleColorRow("Start Color", () => e.ColorStart, c => e.ColorStart = c),
            new ParticleColorRow("End Color", () => e.ColorEnd, c => e.ColorEnd = c)
        };
    }

    private static ParticleVectorRow Vector(string label, Func<Vector3> get, Action<Vector3> set) => new ParticleVectorRow(label, get, set);
}
