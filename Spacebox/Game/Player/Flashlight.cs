﻿using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Common;
using Spacebox.Common.Audio;

namespace Spacebox.Game.Player
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
                spotLight.IsActive = value;
            }
        }

        private SpotLight spotLight;
        private AudioSource audio;
        private Toggi toggle;
        public Flashlight(Camera camera)
        {
            spotLight = new SpotLight(ShaderManager.GetShader("Shaders/block"),
                camera.Front);
            spotLight.UseSpecular = false;

            audio = new AudioSource(SoundManager.GetClip("flashlight"));
            audio.Volume = 0.5f;

            InputManager.AddAction("flashlight", Keys.F);

            InputManager.RegisterCallback("flashlight", () =>
            {
                audio.Play();

                IsActive = !IsActive;
            });

            toggle = ToggleManager.Register("flashlight");
            toggle.OnStateChanged += state => { IsActive = state; };

            
        }

        private void OnToggle(bool state)
        {
            IsActive = state;
        }

        public void Draw(Camera camera)
        {
            spotLight.Draw(camera);
        }
    }
}