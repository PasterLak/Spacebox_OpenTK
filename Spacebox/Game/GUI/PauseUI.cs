using ImGuiNET;
using Spacebox.Common;
using Spacebox.Common.Audio;
using Spacebox.Game.Generation;
using Spacebox.GUI;
using System.Numerics;

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

            RenderMainMenu();
        }

        private static void RenderMainMenu()
        {
            Vector2 windowSize = ImGui.GetIO().DisplaySize;
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(1f, 0.75f, 0f, 0f));
            float windowWidth = windowSize.X * 0.15f;
            float windowHeight = windowSize.Y * 0.3f;
            var windowPos = GameMenu.CenterNextWindow(windowWidth, windowHeight);

            ImGui.Begin("Pause", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse
                | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar);

            float buttonWidth = windowWidth * 0.9f;
            float buttonHeight = windowHeight * 0.12f;
            float spacing = windowHeight * 0.03f;

            GameMenu.DrawElementColors(windowPos, new Vector2(windowWidth, windowHeight), windowSize.Y, 0.005f);

            float totalButtonsHeight = buttonHeight * 3 + spacing * 2;
            ImGui.Dummy(new Vector2(0, (windowHeight - totalButtonsHeight) / 4));

            GameMenu.CenterButtonWithBackground("Continue", buttonWidth, buttonHeight, () => {
                click1?.Play();
                ToggleManager.SetState("pause", false);
            });
            ImGui.Dummy(new Vector2(0, spacing * 2));
            GameMenu.CenterButtonWithBackground(saveButtonText, buttonWidth, buttonHeight, () =>
            {
                click1?.Play();
                if(World.Instance != null)
               World.Instance.SaveWorld();
                saveButtonText = "Saved!";
            });
            ImGui.Dummy(new Vector2(0, spacing * 2));


            GameMenu.CenterButtonWithBackground("Exit", buttonWidth, buttonHeight, () =>
            {
                click1?.Play();
                Window.Instance.Quit();
            });
            ImGui.PopStyleColor(6);
            ImGui.End();
        }


    }
}
