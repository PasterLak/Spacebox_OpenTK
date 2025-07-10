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
        private float _acc;

        public SimulationSpace Space = SimulationSpace.World;
        public int ParticlesCount => _parts.Count;

        private ParticleRenderer _renderer;
        public ParticleSystem(MaterialBase material, EmitterBase emitter)
        {
            Emitter = emitter;
            Emitter.ParticleSystem = this;
            _renderer = new ParticleRenderer(material, Max);
        }

        public void AddParticle(Particle particle) 
        {

        }

        public override void Update()
        {
            _acc += Rate * Time.Delta;
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
            Emitter.Debug();

            var model = Space == SimulationSpace.Local
             ? GetRenderModelMatrix()
            : Matrix4.Identity;

            _renderer.Begin();
            foreach (var p in _parts)
                _renderer.Draw(p, model, Space);
            _renderer.End();
        }

        public void SetEmitter(EmitterBase newEmitter, bool copyCommonParams)
        {
            if (copyCommonParams)
                newEmitter.CopyFrom(Emitter);
            Emitter = newEmitter;
            Emitter.ParticleSystem = this;
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
