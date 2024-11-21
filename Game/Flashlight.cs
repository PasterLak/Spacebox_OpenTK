﻿using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Common;
using Spacebox.Common.Audio;

namespace Spacebox.Game
{
    public class Flashlight
    {
        private bool _isActive = false;

        public bool IsActive
        {
            get { return _isActive; }
            set
            {
                _isActive = value;

                SpotLight.IsActive = value;
            }
        }

        private SpotLight SpotLight;
        private AudioSource audio;
        public Flashlight(Camera camera) 
        {
            SpotLight = new SpotLight(ShaderManager.GetShader("Shaders/block"), 
                camera.Front);
            SpotLight.UseSpecular = false;

            audio = new AudioSource(SoundManager.GetClip("flashlight"));
            audio.Volume = 0.5f;


            InputManager.AddAction("flashlight", Keys.L);
            InputManager.RegisterCallback("flashlight", () =>
            { 
                audio.Play(); 
                SpotLight.IsActive = !SpotLight.IsActive; 
            });
        }

        public void Draw(Camera camera)
        {
            SpotLight.Draw(camera);
        }
    }
}
