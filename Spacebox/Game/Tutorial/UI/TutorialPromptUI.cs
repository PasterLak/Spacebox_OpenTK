using Engine;
using Spacebox.Game.GUI;

namespace Spacebox.Game.Tutorial.UI;

public sealed class TutorialPromptUI : IGameUiController
{
    public const string WindowId = "tutorialPrompt";
    public const string Title = "New to Spacebox?";
    public const string Text = "Start the tutorial? It shows you step by step how to fly, mine, craft, build machines and ships. You can stop it at any time in the pause menu.";

    private readonly TutorialUI _tutorial;
    private readonly WorldLoadingController _loading;
    private bool _shown;

    public TutorialPromptUI(TutorialUI tutorial, WorldLoadingController loading)
    {
        _tutorial = tutorial;
        _loading = loading;
    }

    public bool IsOpen => UIManager.IsOpen(WindowId);

    public void Update()
    {
        if (!_tutorial.IsInitialized || !TutorialService.IsAttached) return;
        if (!TutorialService.NeedsJoinPrompt || _shown || UIManager.IsUIMode || _loading.IsActive) return;

        _shown = true;
        Open();
    }

    public void Reset()
    {
        _shown = false;
    }

    public void Open()
    {
        UIManager.Open(WindowId);
    }

    public void Start()
    {
        if (!IsOpen) return;
        _tutorial.PlayClick();
        TutorialService.Begin();
        UIManager.CloseTop();
    }

    public void Decline()
    {
        if (!IsOpen) return;
        _tutorial.PlayClick();
        TutorialService.Decline();
        UIManager.CloseTop();
    }
}
