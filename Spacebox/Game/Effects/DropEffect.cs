﻿using OpenTK.Mathematics;
using Engine;
using Spacebox.Game.Player;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Effects
{
    public class DropEffect : IDisposable
    {
        public ParticleSystem ParticleSystem { get; private set; }

        private Texture2D dustTexture;
        private Camera camera;

        private float elapsedTime = 0f;
        public const float duration = 20f;

        private Vector3 color = Vector3.One;

        public bool IsFinished => elapsedTime >= duration && ParticleSystem.ParticlesCount == 0;
        public Block Block { get; set; }

        private Vector3 _position;
        public Vector3 Position
        {
            get => _position;
            set
            {
                _position = value;
                if (ParticleSystem != null)
                    ParticleSystem.Position = _position;

            }
        }

        public Vector3 Velocity { get; set; }


        public void Initialize(Astronaut player, Vector3 position, Vector3 color, Texture2D texture, Block block)
        {
            camera = player;
            Position = position;
            this.color = color;
            Block = block;
            Velocity = Vector3.Zero;
            elapsedTime = 0f;
            dustTexture = texture;


            InitializeParticleSystem(position, texture);

        }

        private void InitializeParticleSystem(Vector3 position, Texture2D texture)
        {
            if (ParticleSystem != null)
            {
                //  ParticleSystem.Dispose();
            }

            var emitter = new PointEmitter()
            {
                LifeMin = 20,
                LifeMax = 20,
                StartSizeMin = 0.2f,
                StartSizeMax = 0.2f,
                EndSizeMin = 0.2f,
                EndSizeMax = 0.2f,
                ColorStart = new Vector4(1f, 1f, 1f, 1f),
                ColorEnd = new Vector4(1f, 1f, 1f, 1f),
            
                SpeedMin = 0,
                SpeedMax = 0,
                

            };


            ParticleSystem = new ParticleSystem(new ParticleMaterial(dustTexture), emitter)
            {
                Rate = 100,
                Max = 1
            };
            ParticleSystem.Position = position;

            ParticleSystem.Max = 1;
            ParticleSystem.Space = SimulationSpace.Local;

        }

        public void Update()
        {
            if (elapsedTime < duration)
            {
                elapsedTime += Time.Delta;
                if (elapsedTime >= duration)
                {
                    ParticleSystem.Rate = 0f;
                }
            }

            ParticleSystem.Update();
        }

        public void Render()
        {

             ParticleSystem.Render();
        }

        public void Reset()
        {
            elapsedTime = 0f;
            color = Vector3.One;
            Block = null;
            Position = Vector3.Zero;
            Velocity = Vector3.Zero;
            camera = null;

            if (ParticleSystem != null)
            {
                // ParticleSystem.Dispose();
                ParticleSystem = null;
            }

            dustTexture = null;

        }

        public void Dispose()
        {
            // ParticleSystem?.Dispose();
        }
    }
}
