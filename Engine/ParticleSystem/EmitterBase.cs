using System;
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

        public float StartSizeMin = 0.1f;
        public float StartSizeMax = 0.5f;

        public float EndSizeMin = 0.1f;
        public float EndSizeMax = 0.5f;

        public float SizeMin
        {
            get => StartSizeMin;
            set => StartSizeMin = value;
        }
        public float SizeMax
        {
            get => StartSizeMax;
            set => StartSizeMax = value;
        }

        public Vector4 ColorStart = new Vector4(1f, 1f, 1f, 1f);
        public Vector4 ColorEnd = new Vector4(1f, 1f, 1f, 0f);

        public Vector3 AccelerationStart = Vector3.Zero;
        public Vector3 AccelerationEnd = Vector3.Zero;

        public float RotationSpeedMin = 0f;
        public float RotationSpeedMax = 0f;

        public Node3D ParticleSystem { get; set; }

        public void CopyFrom(EmitterBase o)
        {
            SpeedMin = o.SpeedMin; SpeedMax = o.SpeedMax;
            LifeMin = o.LifeMin; LifeMax = o.LifeMax;
            StartSizeMin = o.StartSizeMin; StartSizeMax = o.StartSizeMax;
            EndSizeMin = o.EndSizeMin; EndSizeMax = o.EndSizeMax;
            ColorStart = o.ColorStart;
            ColorEnd = o.ColorEnd;
            AccelerationStart = o.AccelerationStart;
            AccelerationEnd = o.AccelerationEnd;
            RotationSpeedMin = o.RotationSpeedMin;
            RotationSpeedMax = o.RotationSpeedMax;
        }

        protected float NextFloat() => (float)R.NextDouble();
        protected float Range(float a, float b) => a + (b - a) * NextFloat();

        public abstract Particle Create();
        public abstract void Debug();
    }
}