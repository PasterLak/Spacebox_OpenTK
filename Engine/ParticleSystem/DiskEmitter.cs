
using OpenTK.Mathematics;

namespace Engine
{
    public class DiskEmitter : EmitterBase
    {
        public Vector3 Center = Vector3.Zero;
        public Vector3 Normal = Vector3.UnitY;
        public float Radius = 5f;

        public override Particle Create()
        {
            float rU = MathF.Sqrt(NextFloat()) * Radius;
            float theta = NextFloat() * MathF.PI * 2f;
            var local = new Vector3(rU * MathF.Cos(theta), 0f, rU * MathF.Sin(theta));
            var up = Normal.Normalized();
            var tan = Vector3.Cross(up, Math.Abs(up.X) < 0.99f ? Vector3.UnitX : Vector3.UnitY).Normalized();
            var bit = Vector3.Cross(up, tan);
            var pos = Center + tan * local.X + bit * local.Z;
            var vel = up * Range(SpeedMin, SpeedMax);
            var life = Range(LifeMin, LifeMax);
            var sz = Range(SizeMin, SizeMax);
            var sc = Vector4.Lerp(ColorStart, ColorEnd, NextFloat());
            var ec = Vector4.Lerp(ColorStart, ColorEnd, NextFloat());
            return new Particle(pos, vel, life, sc, ec, sz);
        }

        public override void Debug()
        {
            VisualDebug.DrawDisk(Center, Normal, Radius, Color4.Cyan, 16);
        }
    }
}
