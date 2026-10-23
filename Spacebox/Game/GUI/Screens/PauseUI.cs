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
    public sealed class PauseUI : IGameUiController
    {
        private bool _wasVisible = false;
        private float parallaxIntensity = 0.02f;
        private string saveButtonText = "Save";
        private string networkButtonText = "Open to network";
        private AudioSource click1;
        private const string OptionsWindowName = "pauseOptions";
        private OptionsMenu _options;
        private bool _optionsWasOpen;
        private bool _optionsClosedByMenu;
        private readonly CanvasUnsavedChangesPrompt _leavePrompt = new CanvasUnsavedChangesPrompt(GameCanvasRoot);
        private readonly StatisticsUI _statistics;
        private readonly Spacebox.Game.Tutorial.UI.TutorialMenuUI _tutorialMenu;
        private Action _pendingLeave;
        private bool _interfaceBeforePause = true;

        public PauseUI(StatisticsUI statistics, Spacebox.Game.Tutorial.UI.TutorialMenuUI tutorialMenu)
        {
            _statistics = statistics;
            _tutorialMenu = tutorialMenu;
        }

        public bool IsVisible => UIManager.IsOpen("pause");
        public bool IsOptionsOpen => UIManager.IsOpen(OptionsWindowName);
        public bool IsTutorialOpen => _tutorialMenu.IsOpen;
        public bool IsMultiplayer => IsMultiplayerActive();
        public OptionsMenu Options => _options;
        public string SaveButtonText => saveButtonText;
        public string NetworkButtonText => networkButtonText;
        public float ParallaxIntensity => parallaxIntensity;

        public void Initialize(LocalAstronaut player)
        {
            click1 = new AudioSource(Resources.Load<AudioClip>("click1"), AudioChannel.UI);
            _options = new OptionsMenu(click1, CloseOptions, new CanvasUnsavedChangesPrompt(GameCanvasRoot));
        }

        private static Engine.CanvasUI.CanvasRoot GameCanvasRoot() => Spacebox.Game.GUI.Canvas.GameCanvas.Current?.Root;

        public void OnGUI()
        {
            bool isVisible = IsVisible;

            if (isVisible != _wasVisible)
            {
                if (isVisible)
                {
                    if (!IsMultiplayerActive()) Time.TimeSize = 0;
                    saveButtonText = "Save";
                    networkButtonText = GameSession.IsRemoteClient ? "Connected to server" : Engine.Networking.NetworkManager.Instance.IsListening ? $"Open on port {GameSession.Port}" : "Open to network";
                    _interfaceBeforePause = Settings.ShowInterface;
                    Settings.ShowInterface = false;
                }
                else
                {
                    _statistics.IsVisible = false;
                    Time.TimeSize = 1;
                    saveButtonText = "Save";
                    Settings.ShowInterface = _interfaceBeforePause;
                }
                _wasVisible = isVisible;
            }

            UpdateOptionsState();
        }

        public void Continue()
        {
            click1?.Play();
            UIManager.CloseTop();
        }

        public void Save()
        {
            click1?.Play();
            SaveGame();
            saveButtonText = "Saved!";
        }

        public void OpenOptions()
        {
            click1?.Play();
            _options?.ShowOptions();
            _optionsClosedByMenu = false;
            UIManager.Open(OptionsWindowName);
        }

        public void ToggleStatistics()
        {
            click1?.Play();
            if (_statistics.IsVisible)
            {
                _statistics.Hide();
                return;
            }

            var player = SceneManager.Current.FindNode<LocalAstronaut>();
            if (player != null && player.PlayerStatistics != null) _statistics.Show(player.PlayerStatistics);
        }

        public void OpenTutorial()
        {
            click1?.Play();
            _statistics.Hide();
            _tutorialMenu.Open();
        }

        public void OpenToNetwork()
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

        public void RequestExit()
        {
            click1?.Play();
            if (!TryPromptBeforeLeaving(ExitGame)) ExitGame();
        }

        public void RequestGoToMenuFromButton()
        {
            click1?.Play();
            RequestGoToMenu();
        }

        private void CloseOptions()
        {
            _optionsClosedByMenu = true;
            if (UIManager.IsTop(OptionsWindowName)) UIManager.CloseTop();
        }

        private void UpdateOptionsState()
        {
            bool isOpen = IsOptionsOpen;
            if (_optionsWasOpen && !isOpen && !_optionsClosedByMenu) OptionsMenu.DiscardUnsavedChanges();
            if (!isOpen) _optionsClosedByMenu = false;
            _optionsWasOpen = isOpen;
        }

        public bool TryPromptBeforeLeaving(Action leave)
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

        private void SaveAndLeave()
        {
            click1?.Play();
            SaveGame();
            Leave();
        }

        private void LeaveWithoutSaving()
        {
            click1?.Play();
            Leave();
        }

        private void CancelLeave()
        {
            click1?.Play();
            _pendingLeave = null;
        }

        private void Leave()
        {
            var leave = _pendingLeave;
            _pendingLeave = null;
            leave?.Invoke();
        }

        public void RequestGoToMenu()
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

        public void SetParallaxIntensity(float intensity)
        {
            parallaxIntensity = intensity;
        }

        public void Reset()
        {
            click1?.Dispose();
            click1 = null;
            _options = null;
            _optionsWasOpen = false;
            _optionsClosedByMenu = false;

            if (_wasVisible)
            {
                Time.TimeSize = 1;
                _statistics.IsVisible = false;
            }
            _wasVisible = false;
        }
    }
}
