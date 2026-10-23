using Engine;
using Engine.Audio;
using Spacebox.Game.Player;
using System;

namespace Spacebox.Game.GUI
{
    public class DeathScreen
    {
        public bool IsVisible { get; private set; }

        private float currentTime = 0f;
        private float transitionDuration = 2f;
        private float startAlpha = 0.8f;
        private float endAlpha = 0.4f;
        private OpenTK.Mathematics.Vector3 overlayColor = new OpenTK.Mathematics.Vector3(0.8f, 0.1f, 0.1f);

        private string deathMessage = "YOU DIED";
        private string deathCause = "";

        private AudioSource deathSound;
        private bool soundPlayed = false;
        private float parallaxIntensity = 0.02f;

        public float TransitionDuration
        {
            get => transitionDuration;
            set => transitionDuration = value;
        }

        public float StartAlpha
        {
            get => startAlpha;
            set => startAlpha = OpenTK.Mathematics.MathHelper.Clamp(value, 0f, 1f);
        }

        public float EndAlpha
        {
            get => endAlpha;
            set => endAlpha = OpenTK.Mathematics.MathHelper.Clamp(value, 0f, 1f);
        }

        public string DeathMessage
        {
            get => deathMessage;
            set => deathMessage = value;
        }

        public string DeathCause
        {
            get => deathCause;
            set => deathCause = value;
        }

        public float Progress => transitionDuration <= 0f ? 1f : OpenTK.Mathematics.MathHelper.Clamp(currentTime / transitionDuration, 0f, 1f);

        public float OverlayAlpha => OpenTK.Mathematics.MathHelper.Lerp(startAlpha, endAlpha, Progress);

        public OpenTK.Mathematics.Color4 OverlayColor => new OpenTK.Mathematics.Color4(overlayColor.X, overlayColor.Y, overlayColor.Z, OverlayAlpha);

        public bool ShowsButtons => Progress >= 1f;

        public bool CanReconstruct => OnReconstruct != null;

        public string Blocker => ReconstructionBlocker?.Invoke();

        public float ParallaxIntensity => parallaxIntensity;

        public event Action OnRespawn;
        public event Action OnReconstruct;
        public Func<string> ReconstructionBlocker { get; set; }

        public DeathScreen()
        {
            IsVisible = false;
            deathSound = new AudioSource(GameAssets.LoadResource<AudioClip>("Resources/Audio/death.ogg"));
        }

        public void Show(DeathCase deathCase)
        {
            IsVisible = true;
            currentTime = 0f;
            soundPlayed = false;
            deathCause = deathCase.CauseOfDeath;
        }

        public void Hide()
        {
            IsVisible = false;
            currentTime = 0f;
            soundPlayed = false;
            deathCause = "";
        }

        public void Update()
        {
            if (!IsVisible) return;

            if (!soundPlayed)
            {
                deathSound?.Play();
                soundPlayed = true;
            }

            currentTime += Time.Delta;
        }

        public void Respawn()
        {
            if (!IsVisible) return;
            Hide();
            OnRespawn?.Invoke();
        }

        public void Reconstruct()
        {
            if (!IsVisible || OnReconstruct == null || Blocker != null) return;
            Hide();
            OnReconstruct.Invoke();
        }

        public void SetOverlayColor(float r, float g, float b)
        {
            overlayColor = new OpenTK.Mathematics.Vector3(r, g, b);
        }

        public void SetTransitionParams(float duration, float startAlpha, float endAlpha)
        {
            TransitionDuration = duration;
            StartAlpha = startAlpha;
            EndAlpha = endAlpha;
        }

        public void SetParallaxIntensity(float intensity)
        {
            parallaxIntensity = intensity;
        }
    }
}
