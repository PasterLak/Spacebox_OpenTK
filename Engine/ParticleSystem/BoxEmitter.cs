
using OpenTK.Mathematics;

namespace Engine
{
    public class BoxEmitter : EmitterBase
    {
        public Vector3 Center = Vector3.Zero;
        public Vector3 Size = Vector3.One;
        public Vector3 Direction = Vector3.UnitY;

        public override Particle Create()
        {
            var pos = new Vector3(
                (NextFloat() - 0.5f) * Size.X,
                (NextFloat() - 0.5f) * Size.Y,
                (NextFloat() - 0.5f) * Size.Z
            ) + Center;
            var vel = Direction.Normalized() * Range(SpeedMin, SpeedMax);
            var life = Range(LifeMin, LifeMax);
            var sz = Range(SizeMin, SizeMax);
            var sc = Vector4.Lerp(ColorStart, ColorEnd, NextFloat());
            var ec = Vector4.Lerp(ColorStart, ColorEnd, NextFloat());
            return new Particle(pos, vel, life, sc, ec, sz);
        }

        public override void Debug()
        {
            VisualDebug.DrawBox(Center, Size, Color4.Cyan);
        }
    }
}
