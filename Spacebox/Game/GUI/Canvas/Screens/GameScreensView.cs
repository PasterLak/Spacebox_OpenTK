using Engine.CanvasUI;

namespace Spacebox.Game.GUI.Canvas.Screens;

public sealed class GameScreensView : ScreenOverlay
{
    private readonly List<GameScreen> _screens = new List<GameScreen>();
    private readonly DeathScreenView _death = new DeathScreenView();

    public GameScreensView(GameUi ui)
    {
        Name = "GameScreens";
        Add(new LoadingScreenView(ui.Loading));
        Add(new WelcomeScreen(ui.Welcome));
        Add(new TutorialPromptScreen(ui.TutorialPrompt));
        Add(new ControlsOverlayView());
        Add(new ChatView(ui.Chat, ui.Loading));
        Add(new PauseScreen(ui.Pause, ui.Statistics));
        Add(new TutorialMenuScreen(ui.TutorialMenu, ui.Pause));
        Add(_death);
        Add(new DnaReconstructionView(ui.DnaReconstruction));
        Add(new FadeOverlayView());
    }

    public bool WantsInput
    {
        get
        {
            for (int i = 0; i < _screens.Count; i++)
                if (_screens[i].Visible && _screens[i].WantsInput) return true;
            return false;
        }
    }

    public void Sync(DeathScreen death, float deltaTime)
    {
        _death.Screen = death;
        for (int i = 0; i < _screens.Count; i++) _screens[i].Sync(deltaTime);
    }

    private void Add(GameScreen screen)
    {
        _screens.Add(screen);
        AddChild(screen);
    }
}
