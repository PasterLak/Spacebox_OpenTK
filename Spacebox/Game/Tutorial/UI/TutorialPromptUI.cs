using Engine;

namespace Spacebox.Game.Tutorial.UI;

public static class TutorialPromptUI
{
    public const string WindowId = "tutorialPrompt";
    public const string Title = "New to Spacebox?";
    public const string Text = "Start the tutorial? It shows you step by step how to fly, mine, craft, build machines and ships. You can stop it at any time in the pause menu.";

    public static bool IsOpen => UIManager.IsOpen(WindowId);

    public static void Open()
    {
        UIManager.Open(WindowId);
    }

    public static void Start()
    {
        if (!IsOpen) return;
        TutorialUI.PlayClick();
        TutorialService.Begin();
        UIManager.CloseTop();
    }

    public static void Decline()
    {
        if (!IsOpen) return;
        TutorialUI.PlayClick();
        TutorialService.Decline();
        UIManager.CloseTop();
    }
}
