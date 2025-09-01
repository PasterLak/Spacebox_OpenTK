using Engine;
using Engine.Audio;
using ImGuiNET;
using System.Numerics;

namespace Spacebox.Game.GUI
{
    public class DeathScreen
    {
        public static bool IsVisible = false;

        private float currentTime = 0f;
        private float transitionDuration = 2f;
        private float startAlpha = 0.8f;
        private float endAlpha = 0.4f;
        private Vector4 overlayColor = new Vector4(0.8f, 0.1f, 0.1f, 1f);

        private string deathMessage = "YOU DIED";
        private string respawnButtonText = "RESPAWN";

        private AudioSource deathSound;
        private bool soundPlayed = false;

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

        public event Action OnRespawn;

        public DeathScreen()
        {
            deathSound = new AudioSource(Resources.Get<AudioClip>("Resources/Audio/death.ogg"));
        }

        public void Show()
        {
            IsVisible = true;
            currentTime = 0f;
            soundPlayed = false;
        }

        public void Hide()
        {
            IsVisible = false;
            currentTime = 0f;
            soundPlayed = false;
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

        public void Render()
        {
            if (!IsVisible) return;

            var displaySize = ImGui.GetIO().DisplaySize;

            float progress = OpenTK.Mathematics.MathHelper.Clamp(currentTime / transitionDuration, 0f, 1f);
            float currentAlpha = OpenTK.Mathematics.MathHelper.Lerp(startAlpha, endAlpha, progress);

            ImGui.SetNextWindowPos(Vector2.Zero);
            ImGui.SetNextWindowSize(displaySize);

            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(overlayColor.X, overlayColor.Y, overlayColor.Z, currentAlpha));

            if (ImGui.Begin("DeathOverlay", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoBringToFrontOnFocus))
            {
                ImGui.End();
            }

            ImGui.PopStyleColor();
            ImGui.PopStyleVar(3);

            RenderUI(displaySize, progress);
        }

        private void RenderUI(Vector2 displaySize, float progress)
        {
            ImGui.SetNextWindowPos(Vector2.Zero);
            ImGui.SetNextWindowSize(displaySize);

            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0, 0, 0, 0));

            if (ImGui.Begin("DeathUI", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize))
            {
                float scale = Math.Min(displaySize.X / 1920f, displaySize.Y / 1080f);
                RenderDeathMessage(displaySize, scale);

                if (progress >= 1f)
                {
                    RenderRespawnButton(displaySize, scale);
                }
            }

            ImGui.End();
            ImGui.PopStyleColor();
            ImGui.PopStyleVar(3);
        }

        private void RenderDeathMessage(Vector2 displaySize, float scale)
        {
            float baseFontSize = 72f;
            float scaledFontSize = baseFontSize * scale;

            ImGui.SetWindowFontScale(scaledFontSize / ImGui.GetFontSize());

            Vector2 textSize = ImGui.CalcTextSize(deathMessage);

            float textX = (displaySize.X - textSize.X) * 0.5f;
            float textY = displaySize.Y * 0.25f;

            Vector4 textColor = new Vector4(1f, 0.2f, 0.2f, 1f);
            Vector4 shadowColor = new Vector4(0f, 0f, 0f, 0.8f);

            float shadowOffset = 3f * scale;

            ImGui.SetCursorPos(new Vector2(textX + shadowOffset, textY + shadowOffset));
            ImGui.TextColored(shadowColor, deathMessage);

            ImGui.SetCursorPos(new Vector2(textX, textY));
            ImGui.TextColored(textColor, deathMessage);

            ImGui.SetWindowFontScale(1f);
        }

        private void RenderRespawnButton(Vector2 displaySize, float scale)
        {
            float buttonWidth = 250f * scale;
            float buttonHeight = 60f * scale;

            Vector2 buttonSize = new Vector2(buttonWidth, buttonHeight);
            Vector2 buttonPos = new Vector2(
                (displaySize.X - buttonWidth) * 0.5f,
                displaySize.Y * 0.6f
            );

            ImGui.SetCursorPos(buttonPos);
            Vector2 buttonScreenPos = ImGui.GetCursorScreenPos();

            float offsetValue = buttonHeight * 0.1f;
            Vector2 offset = new Vector2(offsetValue, offsetValue);
            uint borderColor = ImGui.GetColorU32(new Vector4(0.9f, 0.9f, 0.9f, 1f));
            uint lightColor = ImGui.GetColorU32(new Vector4(0.5f, 0.5f, 0.5f, 1f));

            var drawList = ImGui.GetWindowDrawList();
            drawList.AddRectFilled(buttonScreenPos - offset, buttonScreenPos + buttonSize + offset, borderColor);
            drawList.AddRectFilled(buttonScreenPos, buttonScreenPos + buttonSize + offset, lightColor);

            if (ImGui.Button(respawnButtonText, buttonSize))
            {
                OnRespawn?.Invoke();
                Hide();
            }
        }

        public void SetOverlayColor(float r, float g, float b)
        {
            overlayColor = new Vector4(r, g, b, overlayColor.W);
        }

        public void SetTransitionParams(float duration, float startAlpha, float endAlpha)
        {
            TransitionDuration = duration;
            StartAlpha = startAlpha;
            EndAlpha = endAlpha;
        }

    }
}