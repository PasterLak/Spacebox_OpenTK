using OpenTK.Mathematics;

namespace Engine
{
    public enum SimulationSpace : byte
    {
        Local,
        World
    }

    public class ParticleSystem : Node3D
    {
        private readonly HashSet<Particle> _pooledParticles = new();
        private readonly List<Particle> _active = new();
        private readonly Pool<Particle> _particlePool;
        public EmitterBase Emitter { get; private set; }
        private int _max = 500;

        public int Max
        {
            get => _max;
            set
            {
                _max = value;
                _renderer.Rebuild(_max);
                while (_active.Count > _max)
                {
                    var p = _active[^1];
                    _active.RemoveAt(_active.Count - 1);
                    _particlePool.Release(p);
                }
            }
        }

        public float Rate
        {
            get => _rate;
            set
            {
                _rate = value;
                if (_rate <= 0f)
                {
                    _rate = 0;
                    _acc = 0f;
                }
            }
        }

        private float _rate = 50f;
        public float SimulationSpeed = 1f;
        public float Duration = float.PositiveInfinity;
        public bool Loop = false;
        private float _acc;
        private float _time;
        public SimulationSpace Space = SimulationSpace.World;
        public bool UseManually = false;
        public int ParticlesCount => _active.Count;
        private readonly ParticleRenderer _renderer;

        public ParticleRenderer Renderer => _renderer;

        public ParticleMaterial Material
        {
            get => _renderer.Material;
            set => _renderer.Material = value;
        }

        public ParticleSystem(ParticleMaterial material, EmitterBase emitter)
        {
            Emitter = emitter;
            if (Emitter != null)
                Emitter.ParticleSystem = this;

            _particlePool = new Pool<Particle>(
                initialCount: _max,
                initializeFunc: particle => particle,
                onTakeFunc: particle => _pooledParticles.Add(particle),
                resetFunc: particle => particle.Reset(),
                isActiveFunc: particle => particle.Alive,
                setActiveFunc: (particle, active) => { }
            );

            _renderer = new ParticleRenderer(material, Max);
            if (Emitter != null)
                Name = "ParticleSystem_" + emitter.GetType().Name;
        }

        public override void Update()
        {
            base.Update();
            if (UseManually) return;

            float dtSim = Time.Delta * SimulationSpeed;
            _time += dtSim;

            if (_time > Duration)
            {
                if (Loop)
                {
                    ClearParticles();
                    _time = 0f;
                }
                else return;
            }

            if (Rate > 0f)
            {
                _acc += Rate * dtSim;
                while (_acc >= 1f && _active.Count < Max)
                {
                    var p = _particlePool.Take();
                    var tmp = Emitter.Create();

                    p.Position = tmp.Position;
                    p.Velocity = tmp.Velocity;
                    p.Life = tmp.Life;
                    p.Age = 0f;
                    p.ColorStart = tmp.ColorStart;
                    p.ColorEnd = tmp.ColorEnd;
                    p.Color = tmp.ColorStart;
                    p.StartSize = tmp.StartSize;
                    p.EndSize = tmp.EndSize;
                    p.AccStart = tmp.AccStart;
                    p.AccEnd = tmp.AccEnd;
                    p.Rotation = 0f;
                    p.RotationSpeed = tmp.RotationSpeed;

                    PushParticle(p);
                    _acc -= 1f;
                }
            }

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var p = _active[i];
                p.Update();
                if (!p.Alive)
                {
                    _active.RemoveAt(i);
                    if (_pooledParticles.Contains(p))
                    {
                        _particlePool.Release(p);
                        _pooledParticles.Remove(p);
                    }
                }
            }
        }

        public override void Render()
        {
            if (!Enabled) return;
            base.Render();
            Emitter.Debug();
            var model = Space == SimulationSpace.Local ? GetRenderModelMatrix() : Matrix4.Identity;
            _renderer.Begin();
            foreach (var p in _active)
                _renderer.Draw(p, model, Space);
            _renderer.End();
        }

        public Particle CreateParticle()
        {
            var particle = _particlePool.Take();
            return particle;
        }

        public void RemoveParticle(Particle particle)
        {
            if (_active.Contains(particle))
            {
                _active.Remove(particle);
                if (_pooledParticles.Contains(particle))
                {
                    _pooledParticles.Remove(particle);
                    _particlePool.Release(particle);
                }
            }
        }

        public Particle CreateParticle(Vector3 position, Vector3 velocity, float life)
        {
            var particle = CreateParticle();
            particle.Position = position;
            particle.Velocity = velocity;
            particle.Life = life;
            particle.Age = 0f;
            return particle;
        }

        public void PushParticle(Particle particle)
        {
            if (_active.Count >= Max) return;
            if (Space == SimulationSpace.World)
                particle.Position = LocalToWorld(particle.Position);
            _active.Add(particle);
        }

        public void SetEmitter(EmitterBase newEmitter, bool copyCommonParams)
        {
            if (copyCommonParams) newEmitter.CopyFrom(Emitter);
            Emitter = newEmitter;
            Emitter.ParticleSystem = this;
            Name = "ParticleSystem_" + Emitter.GetType().Name;
        }

        public void Prewarm(float seconds)
        {
            if (Rate <= 0f) return;

            float elapsed = 0f;
            bool oldLoop = Loop;
            Loop = false;
            const float step = 0.02f;

            while (elapsed < seconds)
            {
                float dt = Math.Min(step, seconds - elapsed);
                elapsed += dt;
                _acc += Rate * dt;

                while (_acc >= 1f && _active.Count < Max)
                {
                    var p = _particlePool.Take();
                    var tmp = Emitter.Create();

                    p.Position = tmp.Position;
                    p.Velocity = tmp.Velocity;
                    p.Life = tmp.Life;
                    p.Age = 0f;
                    p.ColorStart = tmp.ColorStart;
                    p.ColorEnd = tmp.ColorEnd;
                    p.Color = tmp.ColorStart;
                    p.StartSize = tmp.StartSize;
                    p.EndSize = tmp.EndSize;
                    p.AccStart = tmp.AccStart;
                    p.AccEnd = tmp.AccEnd;
                    p.Rotation = 0f;
                    p.RotationSpeed = tmp.RotationSpeed;

                    if (Space == SimulationSpace.World)
                        p.Position = LocalToWorld(p.Position);

                    _active.Add(p);
                    _acc -= 1f;
                }

                float simDt = dt * SimulationSpeed;
                for (int i = _active.Count - 1; i >= 0; i--)
                {
                    var p = _active[i];
                    p.Age += simDt;
                    float t = MathHelper.Clamp(p.Age / p.Life, 0f, 1f);
                    p.Velocity += Vector3.Lerp(p.AccStart, p.AccEnd, t) * simDt;
                    p.Position += p.Velocity * simDt;
                    p.Rotation += p.RotationSpeed * simDt;
                    p.Color = Vector4.Lerp(p.ColorStart, p.ColorEnd, t);
                    p.Size = MathHelper.Lerp(p.StartSize, p.EndSize, t);
                    if (!p.Alive)
                    {
                        _active.RemoveAt(i);
                        _particlePool.Release(p);
                    }
                }
            }
            _time = seconds;
            Loop = oldLoop;
        }

        public void Restart()
        {
            ClearParticles();
            _time = 0f;
            _acc = 0f;
        }

        public void ClearParticles()
        {
            foreach (var p in _active)
            {
                if (_pooledParticles.Contains(p))
                {
                    _pooledParticles.Remove(p);
                    _particlePool.Release(p);
                }
            }
            _active.Clear();
            _acc = 0f;
        }

        public List<Particle> GetActiveParticles()
        {
            return _active;
        }

        public override void Destroy()
        {
            base.Destroy();
            ClearParticles();
            _renderer.Dispose();
        }
    }
}