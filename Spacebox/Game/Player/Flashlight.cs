using OpenTK.Windowing.GraphicsLibraryFramework;

using Engine.Audio;

using Engine.Light;
using Engine;

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
            var shader = Engine.Resources.Load<Shader>("Shaders/block");
            spotLight = new SpotLight(shader,
                camera.Front);
            spotLight.UseSpecular = false;

            var clip = Engine.Resources.Load<AudioClip>("Resources/Audio/flashlight.ogg");
            audio = new AudioSource(clip);
            audio.Volume = 0.5f;

            InputManager.AddAction("flashlight", Keys.F);

            InputManager.RegisterCallback("flashlight", () =>
            {
                audio.Play();

                IsActive = !IsActive;
            });


            toggle = ToggleManager.Register("flashlight");
            toggle.OnStateChanged += state => 
            { 
                IsActive = state; 
            };

            // use:
            //ToggleManager.SetState("flashlight", true);
        }

        private void OnToggle(bool state)
        {
            IsActive = state;
        }

        public void Draw()
        {
            if(Camera.Main != null)
            spotLight.Render(Camera.Main);
        }
    }
}