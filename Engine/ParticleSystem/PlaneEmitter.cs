// PlaneEmitter.cs
using OpenTK.Mathematics;

namespace Engine
{
    public class PlaneEmitter : EmitterBase
    {
        public Vector3 Center = Vector3.Zero;
        public Vector3 Normal = Vector3.UnitY;
        public float Width = 1f;
        public float Height = 1f;
        public Vector3 Direction = Vector3.UnitY;

        public override Particle Create()
        {
            var up = Normal.Normalized();
            var axis1 = Vector3.Normalize(Vector3.Cross(up, Math.Abs(up.X) < 0.9f ? Vector3.UnitX : Vector3.UnitY));
            var axis2 = Vector3.Normalize(Vector3.Cross(up, axis1));
            float u = (NextFloat() - 0.5f) * Width;
            float v = (NextFloat() - 0.5f) * Height;
            var pos = Center + axis1 * u + axis2 * v;
            var vel = Direction.Normalized() * Range(SpeedMin, SpeedMax);
            var life = Range(LifeMin, LifeMax);
            var startSize = Range(StartSizeMin, StartSizeMax);
            var endSize = Range(EndSizeMin, EndSizeMax);
            var p = new Particle(pos, vel, life, ColorStart, ColorEnd, startSize, endSize);
            p.AccStart = AccelerationStart;
            p.AccEnd = AccelerationEnd;
            p.RotationSpeed = Range(RotationSpeedMin, RotationSpeedMax);
            return p;
        }

        public override void Debug()
        {
            VisualDebug.DrawPlane(Center, Normal, Width, Height, Color4.Cyan);
        }
    }
}
