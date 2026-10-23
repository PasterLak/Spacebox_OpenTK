using Engine.Audio;
using Engine;
using Spacebox.Game.Generation;

namespace Spacebox.Game.GUI
{
    public sealed class WelcomeUI : IGameUiController
    {
        public const string Title = "Welcome to Spacebox!";
        public const string BannerPath = "Resources/Textures/UI/welcome.jpg";
        public const string Text =
            "The game is still in development and, unfortunately, has many bugs and incomplete features.\n\n" +
            "If you find any bugs, please be sure to let me know!\n\nPress F6  to see the controls." +
            "\n\nEnjoy the game!";

        private bool _wasVisible = false;
        private AudioSource clickAudio;
        private bool _playTutorial = true;

        public bool IsVisible => UIManager.IsOpen("welcome");

        public bool HasTutorial => Spacebox.Game.Tutorial.TutorialService.HasTutorialFor(CurrentMode());

        public bool PlayTutorial
        {
            get => _playTutorial;
            set => _playTutorial = value;
        }

        public void Initialize(Spacebox.Game.Player.LocalAstronaut player)
        {
            clickAudio = new AudioSource(Resources.Load<AudioClip>("click1"), AudioChannel.UI);
        }

        public void OnPlayerSpawned(bool savedState)
        {
            if (savedState)
            {
                UIManager.Open("welcome");
                _playTutorial = World.WorldData?.Info?.GameMode == Spacebox.Game.Player.GameModes.GameMode.Survival;

            }
        }

        public void OnGUI()
        {
            bool isVisible = IsVisible;

            if (isVisible != _wasVisible)
            {
                Settings.ShowInterface = !isVisible;
                _wasVisible = isVisible;
            }
        }

        public void Start()
        {
            if (!IsVisible) return;
            bool hasTutorial = HasTutorial;
            clickAudio?.Play();
            UIManager.CloseTop();
            SaveWelcomeState();
            if (!hasTutorial) return;
            if (_playTutorial) Spacebox.Game.Tutorial.TutorialService.Begin();
            else Spacebox.Game.Tutorial.TutorialService.Decline();
        }

        private static Spacebox.Game.Player.GameModes.GameMode CurrentMode()
        {
            return World.Instance?.Player?.GameMode ?? World.WorldData?.Info?.GameMode ?? Spacebox.Game.Player.GameModes.GameMode.Survival;
        }

        private static void SaveWelcomeState()
        {
            if (World.Instance != null)
            {
                var info = World.WorldData.Info;
                info.ShowWelcomeWindow = false;
                WorldInfoSaver.Save(info);
            }
        }

        public void Reset()
        {
            clickAudio?.Dispose();
            clickAudio = null;
        }
    }
}
