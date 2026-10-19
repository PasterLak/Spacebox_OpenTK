using Engine;
using ImGuiNET;
using Spacebox.Game.GUI;
using Spacebox.Game.GUI.Menu;
using System.Numerics;

namespace Spacebox.Game.Tutorial.UI;

public static class TutorialPromptUI
{
    public const string WindowId = "tutorialPrompt";

    private static readonly Vector4 TitleColor = new Vector4(1f, 0.77f, 0.16f, 1f);

    public static void Open()
    {
        UIManager.Open(WindowId);
    }

    public static void OnGUI()
    {
        if (!UIManager.IsOpen(WindowId)) return;

        var display = ImGui.GetIO().DisplaySize;
        float windowWidth = display.X * 0.24f;
        float windowHeight = display.Y * 0.22f;
        var windowPos = GameMenu.CenterNextWindow(windowWidth, windowHeight);

        ImGui.SetNextWindowPos(windowPos);
        ImGui.SetNextWindowSize(new Vector2(windowWidth, windowHeight));
        ImGui.Begin("TutorialPrompt", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar);
        GameMenu.DrawElementColors(windowPos, new Vector2(windowWidth, windowHeight), display.Y, 0.005f);

        float padding = windowWidth * 0.07f;
        ImGui.SetCursorPos(new Vector2(padding, padding * 0.6f));
        ImGui.TextColored(TitleColor, "New to Spacebox?");
        ImGui.SetCursorPosX(padding);
        ImGui.PushTextWrapPos(windowWidth - padding);
        ImGui.TextWrapped("Start the tutorial? It shows you step by step how to fly, mine, craft, build machines and ships. You can stop it at any time in the pause menu.");
        ImGui.PopTextWrapPos();

        float buttonWidth = (windowWidth - padding * 3f) * 0.5f;
        float buttonHeight = windowHeight * 0.2f;
        float buttonY = windowHeight - buttonHeight - padding * 0.6f;

        GameMenu.ButtonWithBackground2("Start", new Vector2(buttonWidth, buttonHeight), new Vector2(padding, buttonY), () =>
        {
            TutorialUI.PlayClick();
            TutorialService.Begin();
            UIManager.CloseTop();
        });

        GameMenu.ButtonWithBackground2("No, thanks", new Vector2(buttonWidth, buttonHeight), new Vector2(padding * 2f + buttonWidth, buttonY), () =>
        {
            TutorialUI.PlayClick();
            TutorialService.Decline();
            UIManager.CloseTop();
        });

        ImGui.End();
    }
}
