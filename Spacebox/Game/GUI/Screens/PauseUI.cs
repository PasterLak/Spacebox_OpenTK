using Engine;
using Engine.Audio;
using Engine.SceneManagement;
using Spacebox.Game.Generation;
using Spacebox.Game.GUI.Menu;
using Spacebox.Game.GUI.MenuUi;
using Spacebox.Game.Player;
using Spacebox.Network;
using Spacebox.Scenes;
using System;

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
        private static readonly CanvasUnsavedChangesPrompt _leavePrompt = new CanvasUnsavedChangesPrompt(GameCanvasRoot);
        private static Action _pendingLeave;

        public static bool IsVisible => UIManager.IsOpen("pause");
        public static bool IsOptionsOpen => UIManager.IsOpen(OptionsWindowName);
        public static bool IsTutorialOpen => Spacebox.Game.Tutorial.UI.TutorialMenuUI.IsOpen;
        public static bool IsMultiplayer => IsMultiplayerActive();
        public static OptionsMenu Options => _options;
        public static string SaveButtonText => saveButtonText;
        public static string NetworkButtonText => networkButtonText;
        public static float ParallaxIntensity => parallaxIntensity;

        public static void Init()
        {
            click1 = new AudioSource(Resources.Load<AudioClip>("click1"), AudioChannel.UI);
            _options = new OptionsMenu(click1, CloseOptions, new CanvasUnsavedChangesPrompt(GameCanvasRoot));
        }

        private static Engine.CanvasUI.CanvasRoot GameCanvasRoot() => Spacebox.Game.GUI.Canvas.GameCanvas.Current?.Root;

        public static void Update()
        {
            bool isVisible = IsVisible;

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
            if (isVisible && !IsOptionsOpen && IsTutorialOpen) Spacebox.Game.Tutorial.UI.TutorialMenuUI.Render();
        }

        public static void Continue()
        {
            click1?.Play();
            UIManager.CloseTop();
        }

        public static void Save()
        {
            click1?.Play();
            SaveGame();
            saveButtonText = "Saved!";
        }

        public static void OpenOptions()
        {
            click1?.Play();
            _options?.ShowOptions();
            _optionsClosedByMenu = false;
            UIManager.Open(OptionsWindowName);
        }

        public static void ToggleStatistics()
        {
            click1?.Play();
            if (StatisticsUI.IsVisible)
            {
                StatisticsUI.Hide();
                return;
            }

            var player = SceneManager.Current.FindNode<LocalAstronaut>();
            if (player != null && player.PlayerStatistics != null) StatisticsUI.Show(player.PlayerStatistics);
        }

        public static void OpenTutorial()
        {
            click1?.Play();
            StatisticsUI.Hide();
            Spacebox.Game.Tutorial.UI.TutorialMenuUI.Open();
        }

        public static void OpenToNetwork()
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
        }

        public static void RequestExit()
        {
            click1?.Play();
            if (!TryPromptBeforeLeaving(ExitGame)) ExitGame();
        }

        public static void RequestGoToMenuFromButton()
        {
            click1?.Play();
            RequestGoToMenu();
        }

        private static void CloseOptions()
        {
            _optionsClosedByMenu = true;
            if (UIManager.IsTop(OptionsWindowName)) UIManager.CloseTop();
        }

        private static void UpdateOptionsState()
        {
            bool isOpen = IsOptionsOpen;
            if (_optionsWasOpen && !isOpen && !_optionsClosedByMenu) OptionsMenu.DiscardUnsavedChanges();
            if (!isOpen) _optionsClosedByMenu = false;
            _optionsWasOpen = isOpen;
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
