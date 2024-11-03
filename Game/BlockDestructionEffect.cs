// BlockDestructionEffect.cs
using OpenTK.Mathematics;
using Spacebox.Common;
using System;
using System.Drawing;

namespace Spacebox.Scenes
{
    public class BlockDestructionEffect : IDisposable
    {
        private ParticleSystem particleSystem;
        private Shader particleShader;
        private Texture2D dustTexture;
        private Camera camera;

        private float elapsedTime = 0f;
        private const float duration = 2f;

        public bool IsFinished => elapsedTime >= duration && particleSystem.GetParticles().Count == 0;

        public BlockDestructionEffect(Camera camera, Vector3 position, Vector3 color)
        {
            this.camera = camera;
            
            Initialize(position);
            particleSystem.Renderer.shader.SetVector3("color", color);
        }

        private void Initialize(Vector3 position)
        {
            dustTexture = new Texture2D("Resources/Textures/blockDust.png", true);

            particleSystem = new ParticleSystem(dustTexture)
            {
                Position = position,
                UseLocalCoordinates = false,
                EmitterPositionOffset = Vector3.Zero,
                //EmitterDirection = new Vector3(0, 1, 0), 
                MaxParticles = 30,
                SpawnRate = 1000f
            };

            var emitter = new Emitter(particleSystem)
            {
                LifetimeMin = 1f,
                LifetimeMax = 2f,
                SizeMin = 0.02f,
                SizeMax = 0.2f,
                StartColorMin = new Vector4(1f, 1f, 1f, 1f),
                StartColorMax = new Vector4(1f, 1f, 1f, 1f),
                EndColorMin = new Vector4(0.8f, 0.5f, 0.5f, 0f),
                EndColorMax = new Vector4(1f, 1f, 0.5f, 0f),
                SpawnRadius = 0.3f,
                SpeedMin = 0.005f,
                SpeedMax = 0.1f,
                EmitterDirection = Vector3.UnitY,
                UseLocalCoordinates = true,
                EnableRandomDirection = true 
            };

            particleSystem.Emitter = emitter;

            particleShader = new Shader("Shaders/particleShader");
            particleShader.Use();
            particleShader.SetInt("particleTexture", 0);
        }

        public void Update()
        {
            if (elapsedTime < duration)
            {
                elapsedTime += Time.Delta;
                if (elapsedTime >= duration)
                {
                    particleSystem.SpawnRate = 0f; 
                }
            }
            particleSystem.Update();

            if(particleSystem.GetParticles().Count == particleSystem.MaxParticles)
            {
                particleSystem.SpawnRate = 0f;
            }
        }

        public void Render()
        {
           
            
            particleSystem.Draw(camera);
        }

        public void Dispose()
        {
            particleSystem.Dispose();
            dustTexture.Dispose();
            particleShader.Dispose();
        }
    }
}
