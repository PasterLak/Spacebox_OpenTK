
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
        private readonly List<Particle> _active = new();
        private readonly Queue<Particle> _pool = new();
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
                    _pool.Enqueue(p);
                }
            }
        }
        public float Rate = 50f;
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
            set
            {
                _renderer.Material = value;
            }
        }

        public ParticleSystem(ParticleMaterial material, EmitterBase emitter)
        {
            Emitter = emitter;
            Emitter.ParticleSystem = this;
            _renderer = new ParticleRenderer(material, Max);

            Name = "ParticleSystem_"+ emitter.GetType().Name;
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
            _acc += Rate * dtSim;
            while (_acc >= 1f && _active.Count < Max)
            {
                Particle p;
                if (_pool.Count > 0) p = _pool.Dequeue();
                else p = new Particle(Vector3.Zero, Vector3.Zero, 1f, Vector4.Zero, Vector4.Zero, 1f, 1f);
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
                if (Space == SimulationSpace.World) p.Position = LocalToWorld(p.Position);
                _active.Add(p);
                _acc -= 1f;
            }
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var p = _active[i];
                p.Update();
                if (!p.Alive)
                {
                    _active.RemoveAt(i);
                    _pool.Enqueue(p);
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

        public void PushParticle(Particle particle)
        {
            if (_active.Count >= Max) return;
            if (Space == SimulationSpace.World) particle.Position = LocalToWorld(particle.Position);
            _active.Add(particle);
        }

        public void SetEmitter(EmitterBase newEmitter, bool copyCommonParams)
        {
            if (copyCommonParams) newEmitter.CopyFrom(Emitter);
            Emitter = newEmitter;
            Emitter.ParticleSystem = this;
        }

        public void Prewarm(float seconds)
        {
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
                    Particle p = _pool.Count > 0 ? _pool.Dequeue() : new Particle(Vector3.Zero, Vector3.Zero, 1f, Vector4.Zero, Vector4.Zero, 1f, 1f);
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
                    if (Space == SimulationSpace.World) p.Position = LocalToWorld(p.Position);
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
                        _pool.Enqueue(p);
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
            foreach (var p in _active) _pool.Enqueue(p);
            _active.Clear();
            _acc = 0f;
        }

        public override void Destroy()
        {
            base.Destroy();
            ClearParticles();
            _renderer.Dispose();
        }
    }
}
