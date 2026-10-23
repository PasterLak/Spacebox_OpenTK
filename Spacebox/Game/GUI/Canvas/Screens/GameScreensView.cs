using Engine.CanvasUI;

namespace Spacebox.Game.GUI.Canvas.Screens;

public sealed class GameScreensView : ScreenOverlay
{
    private readonly List<GameScreen> _screens = new List<GameScreen>();
    private readonly DeathScreenView _death = new DeathScreenView();

    public GameScreensView()
    {
        Name = "GameScreens";
        Add(new LoadingScreenView());
        Add(new WelcomeScreen());
        Add(new TutorialPromptScreen());
        Add(new ControlsOverlayView());
        Add(new PauseScreen());
        Add(new TutorialMenuScreen());
        Add(_death);
        Add(new DnaReconstructionView());
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
