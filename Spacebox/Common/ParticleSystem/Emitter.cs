using OpenTK.Mathematics;

namespace Spacebox.Common
{
    public class Emitter
    {
        private ParticleSystem particleSystem;
        private Random random = new Random();

        public Vector3 Position { get; set; } = Vector3.Zero;
        public float LifetimeMin { get; set; } = 2f;
        public float LifetimeMax { get; set; } = 4f;
        public float SizeMin { get; set; } = 0.5f;
        public float SizeMax { get; set; } = 1f;
        public Vector4 StartColorMin { get; set; } = new Vector4(1f, 1f, 1f, 0f);
        public Vector4 StartColorMax { get; set; } = new Vector4(1f, 1f, 1f, 0f);
        public Vector4 EndColorMin { get; set; } = new Vector4(1f, 1f, 1f, 0f);
        public Vector4 EndColorMax { get; set; } = new Vector4(1f, 1f, 1f, 0f);
        public float SpawnRadius { get; set; } = 50f;
        public float SpeedMin { get; set; } = 0f;
        public float SpeedMax { get; set; } = 0f;

        private bool _randomUVRotation = false;   // -90 90 180
        public bool RandomUVRotation
        {
            get
            {
                return _randomUVRotation;
            }
            set
            { 
                _randomUVRotation = value;
                particleSystem.Renderer.RandomRotation = _randomUVRotation;
            }
        }
     
        public Vector3 EmitterDirection { get; set; } = Vector3.UnitY; 
     
        
        public bool EnableRandomDirection { get; set; } = false;

        public Emitter(ParticleSystem system)
        {
            particleSystem = system;
        }

        public void Emit()
        {
            Vector3 randomPosition = RandomPointInSphere(SpawnRadius);
            Vector3 position = particleSystem.UseLocalCoordinates
     ? randomPosition + particleSystem.EmitterPositionOffset
     : particleSystem.Position + randomPosition + particleSystem.EmitterPositionOffset;


            Vector3 velocity;

            if (EnableRandomDirection)
            {
               
                Vector3 randomDirection = RandomUnitVector();
               
                float speed = MathHelper.Lerp(SpeedMin, SpeedMax, (float)random.NextDouble());
                velocity = randomDirection * speed;
            }
            else
            {
               
                velocity = EmitterDirection * MathHelper.Lerp(SpeedMin, SpeedMax, (float)random.NextDouble());
            }

            float lifetime = MathHelper.Lerp(LifetimeMin, LifetimeMax, (float)random.NextDouble());
            float size = MathHelper.Lerp(SizeMin, SizeMax, (float)random.NextDouble());

            Vector4 startColor = new Vector4(
                MathHelper.Lerp(StartColorMin.X, StartColorMax.X, (float)random.NextDouble()),
                MathHelper.Lerp(StartColorMin.Y, StartColorMax.Y, (float)random.NextDouble()),
                MathHelper.Lerp(StartColorMin.Z, StartColorMax.Z, (float)random.NextDouble()),
                MathHelper.Lerp(StartColorMin.W, StartColorMax.W, (float)random.NextDouble())
            );

            Vector4 endColor = new Vector4(
                MathHelper.Lerp(EndColorMin.X, EndColorMax.X, (float)random.NextDouble()),
                MathHelper.Lerp(EndColorMin.Y, EndColorMax.Y, (float)random.NextDouble()),
                MathHelper.Lerp(EndColorMin.Z, EndColorMax.Z, (float)random.NextDouble()),
                MathHelper.Lerp(EndColorMin.W, EndColorMax.W, (float)random.NextDouble())
            );

            Particle particle = new Particle(position, velocity, lifetime, startColor, endColor, size);
            particleSystem.AddParticle(particle);
        }

        private Vector3 RandomPointInSphere(float radius)
        {
            float u = (float)random.NextDouble();
            float v = (float)random.NextDouble();
            float theta = u * MathF.PI * 2;
            float phi = MathF.Acos(2 * v - 1);
            float r = radius * MathF.Pow((float)random.NextDouble(), 1f / 3f);
            float sinPhi = MathF.Sin(phi);
            float x = r * sinPhi * MathF.Cos(theta);
            float y = r * sinPhi * MathF.Sin(theta);
            float z = r * MathF.Cos(phi);
            return new Vector3(x, y, z);
        }

        private Vector3 RandomUnitVector()
        {
            float theta = (float)(random.NextDouble() * Math.PI * 2);
            float phi = (float)(Math.Acos(2 * random.NextDouble() - 1));
            float x = MathF.Sin(phi) * MathF.Cos(theta);
            float y = MathF.Sin(phi) * MathF.Sin(theta);
            float z = MathF.Cos(phi);
            return new Vector3(x, y, z).Normalized();
        }
    }
}
