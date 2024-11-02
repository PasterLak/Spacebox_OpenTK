// DustSpawner.cs
using OpenTK.Mathematics;
using Spacebox.Common;
using System;

namespace Spacebox.Scenes
{
    public class DustSpawner : IDisposable
    {
        private ParticleSystem particleSystem;
        private Shader particleShader;
        private Texture2D dustTexture;
        private Camera camera;

      
        public Vector3 Position { get; set; } = Vector3.Zero;
        
        public Vector3 EmitterDirection { get; set; } = new Vector3(0, 0, 0);

        public DustSpawner(Camera camera)
        {
            this.camera = camera;
            Initialize();
        }

        private void Initialize()
        {
         
            dustTexture = new Texture2D("Resources/Textures/dust.png", true);

          
            particleSystem = new ParticleSystem(dustTexture)
            {
                Position = Position,
                UseLocalCoordinates = false,
                EmitterDirection = EmitterDirection,
                MaxParticles = 300,
                SpawnRate = 40f
            };


            var emitter = new Emitter(particleSystem)
            {
                SpeedMin = 0f,
                SpeedMax = 0f,
                LifetimeMin = 10f,
                LifetimeMax = 30f,
                SizeMin = 0.1f,
                SizeMax = 0.2f,
                StartColorMin = new Vector4(1f, 1f, 1f, 0f),
                StartColorMax = new Vector4(1f, 1f, 1f, 0f),
                EndColorMin = new Vector4(1f, 1f, 1f, 1f),
                EndColorMax = new Vector4(0.8f, 0.8f, 0.8f, 1f),
                SpawnRadius = 50f,


            };

            particleSystem.Emitter = emitter;

            particleShader = new Shader("Shaders/particleShader");
            particleShader.Use();
            particleShader.SetInt("particleTexture", 0);
        }

        public void Update()
        {
          
            particleSystem.Position = camera.Position;
            particleSystem.Update();

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
