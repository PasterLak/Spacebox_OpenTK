
using OpenTK.Mathematics;
using System.Drawing;

namespace Engine
{
    public class ConeEmitter : EmitterBase
    {
        public Vector3 Apex = Vector3.Zero;
        public Vector3 Axis = Vector3.UnitY;
        public float Angle = 30f;
        public float Height = 1f;

        public override Particle Create()
        {
            float cosA = MathF.Cos(MathHelper.DegreesToRadians(Angle));
            float u = NextFloat();
            float v = NextFloat();
            float theta = 2f * MathF.PI * u;
            float z = MathHelper.Lerp(1f, cosA, v);
            float rc = MathF.Sqrt(1f - z * z);
            var localDir = new Vector3(rc * MathF.Cos(theta), z, rc * MathF.Sin(theta));
            var rotAxis = Vector3.Cross(Vector3.UnitY, Axis).Normalized();
            float rotAng = MathF.Acos(Vector3.Dot(Vector3.UnitY, Axis));
            var dir = Vector3.Transform(localDir, Quaternion.FromAxisAngle(rotAxis, rotAng)).Normalized();
            var dist = NextFloat() * Height;
            var pos = Apex + dir * dist;
            var vel = dir * Range(SpeedMin, SpeedMax);
            var life = Range(LifeMin, LifeMax);
            var sz = Range(SizeMin, SizeMax);
            var sc = Vector4.Lerp(ColorStart, ColorEnd, NextFloat());
            var ec = Vector4.Lerp(ColorStart, ColorEnd, NextFloat());
            return new Particle(pos, vel, life, sc, ec, sz);
        }

        public override void Debug()
        {
            VisualDebug.DrawCone(Apex, Axis, Angle,Height, Color4.Cyan);
        }
    }
}
