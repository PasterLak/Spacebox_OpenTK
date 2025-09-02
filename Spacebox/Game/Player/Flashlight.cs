using OpenTK.Windowing.GraphicsLibraryFramework;

using Engine.Audio;

using Engine.Light;
using Engine;
using OpenTK.Mathematics;
using Engine.InputPro;

namespace Spacebox.Game.Player
{
    public class Flashlight : SpotLight
    {

        private AudioSource audio;
        private Toggi toggle;

        Engine.InputPro.InputAction action;
        public Flashlight()
        {
            GetDirectionFromNode = true;
            Direction = -Vector3.UnitZ;
            Name = "Flashlight";
            Specular = new Vector3(0.5f);
            var clip = Engine.Resources.Load<AudioClip>("Resources/Audio/flashlight.ogg");
            audio = new AudioSource(clip);
            audio.Volume = 0.5f;

            /*InputManager0.AddAction("flashlight", Keys.F);

            InputManager0.RegisterCallback("flashlight", () =>
            {
                audio.Play();

                Enabled = !Enabled;
            });*/


             action = InputManager.Instance.GetAction("flashlight");
            action.Subscribe(InputEventType.Pressed, () =>
            {
                audio.Play();
                Enabled = !Enabled;
               
            });

            toggle = ToggleManager.Register("flashlight");
            toggle.OnStateChanged += state =>
            {
                Enabled = state;
                action.Enabled = state;
            };

            this.Diffuse = new Color3Byte(245, 222, 171).ToVector3();
            this.Specular = new Color3Byte(0,0,0).ToVector3();

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