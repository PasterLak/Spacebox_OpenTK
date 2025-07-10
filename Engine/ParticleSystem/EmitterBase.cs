using OpenTK.Mathematics;

namespace Engine
{
    public abstract class EmitterBase
    {
        protected readonly Random R = new();
        public float SpeedMin = 1f;
        public float SpeedMax = 2f;
        public float LifeMin = 1f;
        public float LifeMax = 3f;
        public float SizeMin = 0.1f;
        public float SizeMax = 0.5f;
        public Vector4 ColorStart = new Vector4(1f, 1f, 1f, 1f);
        public Vector4 ColorEnd = new Vector4(1f, 1f, 1f, 0f);


        public Node3D ParticleSystem { get; set; }

        public void CopyFrom(EmitterBase other)
        {
            SpeedMin = other.SpeedMin;
            SpeedMax = other.SpeedMax;
            LifeMin = other.LifeMin;
            LifeMax = other.LifeMax;
            SizeMin = other.SizeMin;
            SizeMax = other.SizeMax;
            ColorStart = other.ColorStart;
            ColorEnd = other.ColorEnd;
        }

        protected float NextFloat() => (float)R.NextDouble();
        protected float Range(float min, float max) => min + (max - min) * NextFloat();
        public abstract Particle Create();

        public abstract void Debug();
    }
}
