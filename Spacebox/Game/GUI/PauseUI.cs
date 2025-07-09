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
        public static bool IsVisible
        {
            get => _isVisible;
            set
            {
                _isVisible = value;
                //Input.MoveCursorToCenter();
                //ToggleManager.DisableAllWindowsButThis("pause");
                ToggleManager.SetState("mouse", _isVisible);
                ToggleManager.SetState("player", !_isVisible);
                Input.MoveCursorToCenter();
                Settings.ShowInterface = !_isVisible;

                if (_isVisible)
                {
                    Time.TimeSize = 0;
                    saveButtonText = "Save";
                   
                    HealthColorOverlay.SetActive(new Vector3(0,0,0), 0.7f);
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


    }
}
