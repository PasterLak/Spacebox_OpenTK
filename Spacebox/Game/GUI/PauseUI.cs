using ImGuiNET;
using Engine.Audio;
using Engine.SceneManagement;
using Engine;
using Spacebox.Game.Generation;
using Spacebox.Scenes;
using System.Numerics;
using Spacebox.Game.GUI.Menu;

namespace Spacebox.Game.GUI
{
    public class PauseUI
    {
        protected static bool _isVisible = false;
        private static float parallaxIntensity = 0.02f;

        public static bool IsVisible
        {
            get => _isVisible;
            set
            {
                _isVisible = value;

                ToggleManager.SetState("mouse", _isVisible);
                ToggleManager.SetState("player", !_isVisible);
                
                Settings.ShowInterface = !_isVisible;

                if (_isVisible)
                {
                    Time.TimeSize = 0;
                    saveButtonText = "Save";

                    ColorOverlay.FadeOut(new Vector3(0, 0, 0), 0.7f);
                }
                else
                {
                    Time.TimeSize = 1;
                    saveButtonText = "Save";
                }

            }
        }
        private static string saveButtonText = "Save";
        private static AudioSource click1;


        public static void Init()
        {
            var toggle = ToggleManager.Register("pause");
            toggle.IsUI = true;
            toggle.OnStateChanged += (s) =>
            {
                IsVisible = s;
            };
        }


        public static void OnGUI()
        {
            if (!_isVisible) return;

           
            RenderPause(); 
            RenderPauseTitle();
        }

        private static void RenderPauseTitle()
        {
            Vector2 displaySize = ImGui.GetIO().DisplaySize;

            ImGui.SetNextWindowPos(Vector2.Zero);
            ImGui.SetNextWindowSize(displaySize);

            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0, 0, 0, 0));

            if (ImGui.Begin("PauseTitle", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoBringToFrontOnFocus))
            {
                float scale = Math.Min(displaySize.X / 1920f, displaySize.Y / 1080f);
                float baseFontSize = 72f;
                float scaledFontSize = baseFontSize * scale;

                ImGui.SetWindowFontScale(scaledFontSize / ImGui.GetFontSize());

                const string pauseText = "PAUSE";
                Vector2 textSize = ImGui.CalcTextSize(pauseText);

                Vector2 mousePosition = Input.Mouse.Position.ToSystemVector2();
                mousePosition.X = 0;
                Vector2 offset = (mousePosition - displaySize / 2f) * parallaxIntensity;

                float textX = (displaySize.X - textSize.X) * 0.5f + offset.X;
                float textY = displaySize.Y * 0.15f + offset.Y;

                Vector4 textColor = new Vector4(1f, 1f, 1f, 1f);
                Vector4 shadowColor = new Vector4(0.949f, 0.9f, 0.461f,0.8f);

                float shadowOffset = 4f * scale;

                ImGui.SetCursorPos(new Vector2(textX + shadowOffset, textY + shadowOffset));
                ImGui.TextColored(shadowColor, pauseText);

                ImGui.SetCursorPos(new Vector2(textX, textY ));
                ImGui.TextColored(textColor, pauseText);

                ImGui.SetWindowFontScale(1f);
            }

            ImGui.End();
            ImGui.PopStyleColor();
            ImGui.PopStyleVar(3);
        }

        private static void RenderPause()
        {
            Vector2 displaySize = ImGui.GetIO().DisplaySize;
            float windowWidth = displaySize.X * 0.15f;
            float windowHeight = displaySize.Y * 0.3f;
            Vector2 windowPos = GameMenu.CenterNextWindow(windowWidth, windowHeight);
            ImGui.SetNextWindowPos(windowPos);
            ImGui.SetNextWindowSize(new Vector2(windowWidth, windowHeight));
            ImGui.Begin("Pause", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar);
            float buttonWidth = windowWidth * 0.9f;
            float buttonHeight = windowHeight * 0.12f;
            GameMenu.DrawElementColors(windowPos, new Vector2(windowWidth, windowHeight), displaySize.Y, 0.005f);
            int buttonCount = 4;
            float spacing = (windowHeight - (buttonCount * buttonHeight)) / (buttonCount + 1);
            float currentY = spacing;
            ImGui.SetCursorPos(new Vector2((windowWidth - buttonWidth) / 2, currentY));
            GameMenu.CenterButtonWithBackground("Continue", buttonWidth, buttonHeight, () =>
            {
                click1?.Play();
                ToggleManager.SetState("pause", false);
                ToggleManager.SetState("panel", true);
            });
            currentY += buttonHeight + spacing;
            ImGui.SetCursorPos(new Vector2((windowWidth - buttonWidth) / 2, currentY));
            GameMenu.CenterButtonWithBackground(saveButtonText, buttonWidth, buttonHeight, () =>
            {
                click1?.Play();
                if (World.Instance != null)
                    World.Instance.Save();
                saveButtonText = "Saved!";
            });
            currentY += buttonHeight + spacing;
            ImGui.SetCursorPos(new Vector2((windowWidth - buttonWidth) / 2, currentY));
            GameMenu.CenterButtonWithBackground("Go to menu", buttonWidth, buttonHeight, () =>
            {
                click1?.Play();
                ToggleManager.SetState("pause", false);
                ToggleManager.SetState("panel", true);
                SceneManager.Load<MenuScene>();
            });
            currentY += buttonHeight + spacing;
            ImGui.SetCursorPos(new Vector2((windowWidth - buttonWidth) / 2, currentY));
            GameMenu.CenterButtonWithBackground("Exit", buttonWidth, buttonHeight, () =>
            {
                click1?.Play();
                Window.Instance.Quit();
            });
            ImGui.End();
        }

        public static void SetParallaxIntensity(float intensity)
        {
            parallaxIntensity = intensity;
        }

        public static void Dispose()
        {
            click1?.Dispose();
            click1 = null;
            _isVisible = false;

        }
    }
}