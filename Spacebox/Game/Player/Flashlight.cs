using OpenTK.Windowing.GraphicsLibraryFramework;

using Engine.Audio;

using Engine.Light;
using Engine;
using OpenTK.Mathematics;

namespace Spacebox.Game.Player
{
    public class Flashlight : SpotLight
    {
  
        private AudioSource audio;
        private Toggi toggle;
        public Flashlight()
        {
            GetDirectionFromNode = true;
            Direction = -Vector3.UnitZ;
            Name = "Flashlight";
            Specular = new Vector3 (0.5f);
            var clip = Engine.Resources.Load<AudioClip>("Resources/Audio/flashlight.ogg");
            audio = new AudioSource(clip);
            audio.Volume = 0.5f;

            InputManager.AddAction("flashlight", Keys.F);

            InputManager.RegisterCallback("flashlight", () =>
            {
                audio.Play();

                Enabled = !Enabled;
            });


            toggle = ToggleManager.Register("flashlight");
            toggle.OnStateChanged += state => 
            {
                Enabled = state; 
            };

            // use:
            //ToggleManager.SetState("flashlight", true);
        }

        public override void Update()
        {
            base.Update();
            Direction = Parent.ForwardLocal;
        }
        private void OnToggle(bool state)
        {
            Enabled = state;
        }

    }
}