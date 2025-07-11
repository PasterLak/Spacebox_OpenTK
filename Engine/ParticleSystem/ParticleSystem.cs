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
        private readonly List<Particle> _parts = new();
        public EmitterBase Emitter { get; private set; }
        private int _max = 500;
        public int Max
        {
            get => _max;
            set
            {
                _max = value;
                _renderer.Rebuild(_max);
                if (_parts.Count > _max)
                    _parts.RemoveRange(_max, _parts.Count - _max);
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
        public int ParticlesCount => _parts.Count;

        private readonly ParticleRenderer _renderer;

        public ParticleSystem(MaterialBase material, EmitterBase emitter)
        {
            Emitter = emitter;
            Emitter.ParticleSystem = this;
            _renderer = new ParticleRenderer(material, Max);
        }

        public override void Update()
        {

            base.Update();
            if (UseManually) return;

            float dt = Time.Delta * SimulationSpeed;
            _time += dt;
            if (_time > Duration)
            {
                if (Loop)
                {
                    ClearParticles();
                    _time = 0f;
                }
                else return;
            }

            _acc += Rate * dt;
            while (_acc >= 1f && _parts.Count < Max)
            {
                var p = Emitter.Create();
                if (Space == SimulationSpace.World)
                    p.Position = LocalToWorld(p.Position);
                _parts.Add(p);
                _acc -= 1f;
            }

            for (int i = _parts.Count - 1; i >= 0; i--)
            {
                var p = _parts[i];
                p.Update();
                if (!p.Alive) _parts.RemoveAt(i);
            }
        }

        public override void Render()
        {
            if(!Enabled) return;
            base.Render();

            Emitter.Debug();
            var model = Space == SimulationSpace.Local
                ? GetRenderModelMatrix()
                : Matrix4.Identity;
            _renderer.Begin();
            foreach (var p in _parts)
                _renderer.Draw(p, model, Space);
            _renderer.End();
        }

        public void PushParticle(Particle particle)
        {
            if (_parts.Count >= Max)
                return;

            if (Space == SimulationSpace.World)
                particle.Position = LocalToWorld(particle.Position);

            _parts.Add(particle);
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
                while (_acc >= 1f && _parts.Count < Max)
                {
                    var p = Emitter.Create();
                    if (Space == SimulationSpace.World)
                        p.Position = LocalToWorld(p.Position);
                    _parts.Add(p);
                    _acc -= 1f;
                }

                float simDt = dt * SimulationSpeed;
                for (int i = _parts.Count - 1; i >= 0; i--)
                {
                    var p = _parts[i];
   
                    p.Age += simDt;
                    float t = MathHelper.Clamp(p.Age / p.Life, 0f, 1f);
                    p.Velocity += Vector3.Lerp(p.AccStart, p.AccEnd, t) * simDt;
                    p.Position += p.Velocity * simDt;
                    p.Rotation += p.RotationSpeed * simDt;
                    p.Color = Vector4.Lerp(p.ColorStart, p.ColorEnd, t);
                    p.Size = MathHelper.Lerp(p.StartSize, p.EndSize, t);

                    if (!p.Alive)
                        _parts.RemoveAt(i);
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
            _parts.Clear();
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
