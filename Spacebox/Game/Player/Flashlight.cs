using OpenTK.Windowing.GraphicsLibraryFramework;

using Engine.Audio;

using Engine.Light;
using Engine;
using OpenTK.Mathematics;
using Engine.InputPro;
using Spacebox.Game.GUI;

namespace Spacebox.Game.Player
{
    public class Flashlight : SpotLight
    {

        private AudioSource audio;
        private Toggi toggle;

        Engine.InputPro.InputAction action;
        public Flashlight(Astronaut astronaut)
        {
            GetDirectionFromNode = true;
            Direction = -Vector3.UnitZ;
            Name = "Flashlight";
            Specular = new Vector3(0.5f);
            var clip = Engine.Resources.Load<AudioClip>("Resources/Audio/flashlight.ogg");
            audio = new AudioSource(clip);
            audio.Volume = 0.5f;

            action = InputManager.Instance.GetAction("flashlight");
            action.Subscribe(InputEventType.Pressed, () =>
            {
                if (astronaut.IsAlive == false) return;
                if (ToggleManager.OpenedWindowsCount > 0) return;
                if (Debug.IsVisible) return;
                if (Chat.FocusInput) return;

                audio.Play();
                Enabled = !Enabled;

            });

            toggle = ToggleManager.Register("flashlight");
            toggle.OnStateChanged += state =>
            {
                if (astronaut.IsAlive == false) return;
                Enabled = state;
                action.Enabled = state;
            };

            this.Diffuse = new Color3Byte(245, 222, 171).ToVector3();
            this.Specular = new Color3Byte(0, 0, 0).ToVector3();

        }

        public override void Update()
        {
            base.Update();
            Direction = Parent.ForwardLocal;
        }
 

    }
}