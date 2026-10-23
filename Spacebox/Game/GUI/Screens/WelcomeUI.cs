using Engine.Audio;
using Engine;
using Spacebox.Game.Generation;

namespace Spacebox.Game.GUI
{
    public class WelcomeUI
    {
        public const string Title = "Welcome to Spacebox!";
        public const string BannerPath = "Resources/Textures/UI/welcome.jpg";
        public const string Text =
            "The game is still in development and, unfortunately, has many bugs and incomplete features.\n\n" +
            "If you find any bugs, please be sure to let me know!\n\nPress F6  to see the controls." +
            "\n\nEnjoy the game!";

        private static bool _wasVisible = false;
        private static AudioSource clickAudio;
        private static bool _playTutorial = true;

        public static bool IsVisible => UIManager.IsOpen("welcome");

        public static bool HasTutorial => Spacebox.Game.Tutorial.TutorialService.HasTutorialFor(CurrentMode());

        public static bool PlayTutorial
        {
            get => _playTutorial;
            set => _playTutorial = value;
        }

        public static void Init()
        {
            clickAudio = new AudioSource(Resources.Load<AudioClip>("click1"), AudioChannel.UI);
        }

        public static void OnPlayerSpawned(bool savedState)
        {
            if (savedState)
            {
                UIManager.Open("welcome");
                _playTutorial = World.WorldData?.Info?.GameMode == Spacebox.Game.Player.GameModes.GameMode.Survival;

            }
        }

        public static void Update()
        {
            bool isVisible = IsVisible;

            if (isVisible != _wasVisible)
            {
                Settings.ShowInterface = !isVisible;
                _wasVisible = isVisible;
            }
        }

        public static void Start()
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

        public static void Dispose()
        {
            clickAudio?.Dispose();
            clickAudio = null;
        }
    }
}
