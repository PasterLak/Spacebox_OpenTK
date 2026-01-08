
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

        private Engine.InputPro.InputAction action;
        public Flashlight()
        {
            GetDirectionFromNode = true;
            Direction = -Vector3.UnitZ;
            Name = "Flashlight";
            Specular = new Vector3(0.5f);
            var clip = Engine.Resources.Load<AudioClip>("Resources/Audio/flashlight.ogg");
            audio = new AudioSource(clip);
            audio.Volume = 0.5f;

            

            this.Diffuse = new Color3Byte(245, 222, 171).ToVector3(); // was 245, 222, 171
            this.Specular = new Color3Byte(0, 0, 0).ToVector3(); // was 0

        }

        public void AddToggleToManager(LocalAstronaut localAstronaut)
        {
            action = Input.GetAction("flashlight");
            action.Subscribe(InputEventType.Pressed, () =>
            {
                if (localAstronaut.IsAlive == false) return;
                if (ToggleManager.OpenedWindowsCount > 0) return;
                if (Debug.IsVisible) return;
                if (Chat.FocusInput) return;

                localAstronaut.PlayerStatistics.FlashlightToggles++;
                audio.Play();
                Enabled = !Enabled;

            });

            toggle = ToggleManager.Register("flashlight");
            toggle.OnStateChanged += state =>
            {
                if (localAstronaut.IsAlive == false) return;
                Enabled = state;
                action.Enabled = state;

            };
        }

        public override void Update()
        {
            base.Update();
            Direction = Parent.ForwardLocal;
        }

        public override void Destroy()
        {
            base.Destroy();
           
        }
 

    }
}