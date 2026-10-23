using Engine;
using Engine.Audio;
using Engine.SceneManagement;
using ImGuiNET;
using Spacebox.Game.Generation;
using Spacebox.Game.GUI.Menu;
using Spacebox.Game.Player;
using Spacebox.Network;
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
        private static string networkButtonText = "Open to network";
        private static AudioSource click1;
        private const string OptionsWindowName = "pauseOptions";
        private static OptionsMenu _options;
        private static bool _optionsWasOpen;
        private static bool _optionsClosedByMenu;
        private static readonly uint DimColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0f, 0f, 0f, 0.7f));
        private static readonly UnsavedChangesPrompt _leavePrompt = new UnsavedChangesPrompt();
        private static Action _pendingLeave;

        public static void Init()
        {
            click1 = new AudioSource(Resources.Load<AudioClip>("click1"), AudioChannel.UI);
            _options = new OptionsMenu(click1, CloseOptions);
        }

        public static void OnGUI()
        {
            bool isVisible = UIManager.IsOpen("pause");

            if (isVisible != _wasVisible)
            {
                if (isVisible)
                {
                    if (!IsMultiplayerActive()) Time.TimeSize = 0;
                    saveButtonText = "Save";
                    networkButtonText = GameSession.IsRemoteClient ? "Connected to server" : Engine.Networking.NetworkManager.Instance.IsListening ? $"Open on port {GameSession.Port}" : "Open to network";
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

            UpdateOptionsState();
            if (!isVisible) return;

            ImGui.GetBackgroundDrawList().AddRectFilled(Vector2.Zero, ImGui.GetIO().DisplaySize, DimColor);
            if (UIManager.IsOpen(OptionsWindowName))
            {
                using (MenuStyle.Begin())
                    _options?.Render();
                return;
            }

            if (Spacebox.Game.Tutorial.UI.TutorialMenuUI.IsOpen)
            {
                Spacebox.Game.Tutorial.UI.TutorialMenuUI.Render();
                return;
            }

            RenderPause();
            _leavePrompt.Render();
            if (IsMultiplayerActive()) PlayerListUI.Draw();
            RenderPauseTitle();
            StatisticsUI.OnGUI();
        }

        private static void OpenOptions()
        {
            _options?.ShowOptions();
            _optionsClosedByMenu = false;
            UIManager.Open(OptionsWindowName);
        }

        private static void CloseOptions()
        {
            _optionsClosedByMenu = true;
            if (UIManager.IsTop(OptionsWindowName)) UIManager.CloseTop();
        }

        private static void UpdateOptionsState()
        {
            bool isOpen = UIManager.IsOpen(OptionsWindowName);
            if (_optionsWasOpen && !isOpen && !_optionsClosedByMenu) OptionsMenu.DiscardUnsavedChanges();
            if (!isOpen) _optionsClosedByMenu = false;
            _optionsWasOpen = isOpen;
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
            float windowHeight = displaySize.Y * 0.4f;
            Vector2 windowPos = GameMenu.CenterNextWindow(windowWidth, windowHeight);

            ImGui.SetNextWindowPos(windowPos);
            ImGui.SetNextWindowSize(new Vector2(windowWidth, windowHeight));
            ImGui.Begin("Pause", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar);

            float buttonWidth = windowWidth * 0.9f;
            float buttonHeight = windowHeight * 0.105f;
            GameMenu.DrawElementColors(windowPos, new Vector2(windowWidth, windowHeight), displaySize.Y, 0.005f);

            const int buttonCount = 8;
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
                SaveGame();
                saveButtonText = "Saved!";
            });

            currentY += buttonHeight + spacing;
            ImGui.SetCursorPos(new Vector2((windowWidth - buttonWidth) / 2, currentY));
            GameMenu.CenterButtonWithBackground("Options", buttonWidth, buttonHeight, () =>
            {
                click1?.Play();
                OpenOptions();
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
            GameMenu.CenterButtonWithBackground("Tutorial", buttonWidth, buttonHeight, () =>
            {
                click1?.Play();
                StatisticsUI.Hide();
                Spacebox.Game.Tutorial.UI.TutorialMenuUI.Open();
            });

            currentY += buttonHeight + spacing;
            ImGui.SetCursorPos(new Vector2((windowWidth - buttonWidth) / 2, currentY));
            GameMenu.CenterButtonWithBackground(networkButtonText, buttonWidth, buttonHeight, () =>
            {
                click1?.Play();
                var session = NetworkSession.Current;
                var network = Engine.Networking.NetworkManager.Instance;
                if (session == null || !network.IsServer || network.IsListening) return;

                if (session.OpenToNetwork(GameSession.Port))
                {
                    networkButtonText = $"Open on port {GameSession.Port}";
                    Time.TimeSize = 1;
                }
                else
                {
                    networkButtonText = "Port is busy";
                }
            });

            currentY += buttonHeight + spacing;
            ImGui.SetCursorPos(new Vector2((windowWidth - buttonWidth) / 2, currentY));
            GameMenu.CenterButtonWithBackground("Go to menu", buttonWidth, buttonHeight, () =>
            {
                click1?.Play();
                if (!TryPromptBeforeLeaving(GoToMenu)) GoToMenu();
            });

            currentY += buttonHeight + spacing;
            ImGui.SetCursorPos(new Vector2((windowWidth - buttonWidth) / 2, currentY));
            GameMenu.CenterButtonWithBackground("Exit", buttonWidth, buttonHeight, () =>
            {
                click1?.Play();
                if (!TryPromptBeforeLeaving(ExitGame)) ExitGame();
            });

            ImGui.End();
        }

        public static bool TryPromptBeforeLeaving(Action leave)
        {
            if (World.Instance == null || !World.Instance.HasUnsavedChanges()) return false;
            if (WorldSavePolicy.SavesOnLeave)
            {
                SaveGame();
                return false;
            }

            _pendingLeave = leave;
            if (!UIManager.IsOpen("pause")) UIManager.Open("pause");
            _leavePrompt.Open(SaveAndLeave, LeaveWithoutSaving, CancelLeave);
            return true;
        }

        private static void SaveGame()
        {
            if (World.Instance == null) return;
            if (NetworkSession.Current != null && !GameSession.IsRemoteClient) NetworkSession.Current.SaveWorld("Save");
            else World.Instance.Save();
        }

        private static void SaveAndLeave()
        {
            click1?.Play();
            SaveGame();
            Leave();
        }

        private static void LeaveWithoutSaving()
        {
            click1?.Play();
            Leave();
        }

        private static void CancelLeave()
        {
            click1?.Play();
            _pendingLeave = null;
        }

        private static void Leave()
        {
            var leave = _pendingLeave;
            _pendingLeave = null;
            leave?.Invoke();
        }

        public static void RequestGoToMenu()
        {
            if (!TryPromptBeforeLeaving(GoToMenu)) GoToMenu();
        }

        private static void GoToMenu()
        {
            UIManager.CloseAll();
            SceneManager.Load<MenuScene>();
        }

        private static void ExitGame()
        {
            SpaceboxWindow.Instance.Quit();
        }

        private static bool IsMultiplayerActive()
        {
            var network = Engine.Networking.NetworkManager.Instance;
            return network.IsRemoteClient || network.IsListening;
        }

        public static void SetParallaxIntensity(float intensity)
        {
            parallaxIntensity = intensity;
        }

        public static void Dispose()
        {
            click1?.Dispose();
            click1 = null;
            _options = null;
            _optionsWasOpen = false;
            _optionsClosedByMenu = false;

            if (_wasVisible)
            {
                Time.TimeSize = 1;
                StatisticsUI.IsVisible = false;
            }
            _wasVisible = false;
        }
    }
}