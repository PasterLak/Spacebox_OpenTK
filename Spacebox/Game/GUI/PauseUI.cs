using Engine;
using Engine.Audio;
using Engine.SceneManagement;
using ImGuiNET;
using Spacebox.Game.Generation;
using Spacebox.Game.GUI.Menu;
using Spacebox.Game.Player;
using Spacebox.Scenes;
using System;
using System.Numerics;

namespace Spacebox.Game.GUI
{
    public class PauseUI
    {
        private static bool _wasVisible = false;
        private static float parallaxIntensity = 0.02f;
        private static string saveButtonText = "Save";
        private static AudioSource click1;

        public static void Init()
        {
            click1 = new AudioSource(Resources.Load<AudioClip>("click1"));
        }

        public static void OnGUI()
        {
            bool isVisible = UIManager.IsOpen("pause");

            if (isVisible != _wasVisible)
            {
                if (isVisible)
                {
                    Time.TimeSize = 0;
                    saveButtonText = "Save";
                    ColorOverlay.FadeOut(new Vector3(0, 0, 0), 0.7f);
                    Settings.ShowInterface = false;
                }
                else
                {
                    StatisticsUI.IsVisible = false;
                    Time.TimeSize = 1;
                    saveButtonText = "Save";
                    Settings.ShowInterface = true;
                }
                _wasVisible = isVisible;
            }

            if (!isVisible) return;

            RenderPause();
            RenderPauseTitle();
            StatisticsUI.OnGUI();
        }

        private static void RenderPauseTitle()
        {
            Vector2 displaySize = ImGui.GetIO().DisplaySize;
            float scale = Math.Min(displaySize.X / 1920f, displaySize.Y / 1080f);
            float scaledFontSize = 120f * scale;
            const string pauseText = "PAUSE";

            var font = ImGui.GetFont();
            Vector2 textSize = font.CalcTextSizeA(scaledFontSize, float.MaxValue, 0f, pauseText);

            Vector2 mousePosition = Input.Mouse.Position.ToSystemVector2();
            Vector2 offset = (mousePosition - displaySize / 2f) * parallaxIntensity;

            float textX = (displaySize.X - textSize.X) * 0.5f + offset.X;
            float textY = displaySize.Y * 0.15f + offset.Y;
            Vector2 pos = new Vector2(textX, textY);

            uint textColor = ImGui.GetColorU32(new Vector4(250/255f, 186/255f, 0f, 1f));
            uint shadowColor1 = ImGui.GetColorU32(new Vector4(86 / 255f, 69 / 255f, 17 / 255f, 0.8f));
            uint shadowColor2 = ImGui.GetColorU32(new Vector4(40 / 255f, 30 / 255f, 5 / 255f, 0.8f));

            float shadowOffset1 = 4f * scale;
            float shadowOffset2 = 8f * scale;

            var drawList = ImGui.GetForegroundDrawList();

            drawList.AddText(font, scaledFontSize, pos + new Vector2(shadowOffset2, shadowOffset2), shadowColor2, pauseText);
            drawList.AddText(font, scaledFontSize, pos + new Vector2(shadowOffset1, shadowOffset1), shadowColor1, pauseText);
            drawList.AddText(font, scaledFontSize, pos, textColor, pauseText);
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

            const int buttonCount = 5;
            float spacing = (windowHeight - (buttonCount * buttonHeight)) / (buttonCount + 1);
            float currentY = spacing;

            ImGui.SetCursorPos(new Vector2((windowWidth - buttonWidth) / 2, currentY));
            GameMenu.CenterButtonWithBackground("Continue", buttonWidth, buttonHeight, () =>
            {
                click1?.Play();
                UIManager.CloseTop();
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
            GameMenu.CenterButtonWithBackground("Statistics", buttonWidth, buttonHeight, () =>
            {
                click1?.Play();

                var player = SceneManager.Current.FindNode<LocalAstronaut>();
                if (player != null && player.PlayerStatistics != null)
                {
                    StatisticsUI.Show(player.PlayerStatistics);
                }
            });

            currentY += buttonHeight + spacing;
            ImGui.SetCursorPos(new Vector2((windowWidth - buttonWidth) / 2, currentY));
            GameMenu.CenterButtonWithBackground("Go to menu", buttonWidth, buttonHeight, () =>
            {
                click1?.Play();
                UIManager.CloseAll();
                SceneManager.Load<MenuScene>();
            });

            currentY += buttonHeight + spacing;
            ImGui.SetCursorPos(new Vector2((windowWidth - buttonWidth) / 2, currentY));
            GameMenu.CenterButtonWithBackground("Exit", buttonWidth, buttonHeight, () =>
            {
                click1?.Play();
                SpaceboxWindow.Instance.Quit();
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
            _wasVisible = false;
        }
    }
}