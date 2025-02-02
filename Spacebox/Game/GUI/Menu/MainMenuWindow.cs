
using ImGuiNET;
using Spacebox;
using Spacebox.Game.GUI.Menu;
using System.Numerics;

public abstract class MenuWindow
{
    public abstract void Render();
}

public class MainMenuWindow : MenuWindow
{
    private GameMenu menu;
    public MainMenuWindow(GameMenu menu)
    {
        this.menu = menu;
    }
    public override void Render()
    {
        Vector2 windowSize = ImGui.GetIO().DisplaySize;
        float windowWidth = windowSize.X * 0.15f;
        float windowHeight = windowSize.Y * 0.3f;
        Vector2 windowPos = GameMenu.CenterNextWindow2(windowWidth, windowHeight);
        ImGui.SetNextWindowPos(windowPos);
        ImGui.SetNextWindowSize(new Vector2(windowWidth, windowHeight));
        ImGui.Begin("Main Menu", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove
            | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar);
        float buttonWidth = windowWidth * 0.9f;
        float buttonHeight = windowHeight * 0.12f;
        GameMenu.DrawElementColors(windowPos, new Vector2(windowWidth, windowHeight), windowSize.Y, 0.005f);
        int buttonCount = 4;
        float spacing = (windowHeight - (buttonCount * buttonHeight)) / (buttonCount + 1);
        float currentY = spacing;
        ImGui.SetCursorPos(new Vector2((windowWidth - buttonWidth) / 2, currentY));
        GameMenu.CenterButtonWithBackground("Play", buttonWidth, buttonHeight, () =>
        {
            menu.click1.Play();
            menu.SetStateToWorldSelect();
        });
        currentY += buttonHeight + spacing;
        ImGui.SetCursorPos(new Vector2((windowWidth - buttonWidth) / 2, currentY));
        GameMenu.CenterButtonWithBackground("Multiplayer", buttonWidth, buttonHeight, () =>
        {
            menu.click1.Play();
            menu.SetStateToMultiplayer();
        });
        currentY += buttonHeight + spacing;
        ImGui.SetCursorPos(new Vector2((windowWidth - buttonWidth) / 2, currentY));
        GameMenu.CenterButtonWithBackground("Options", buttonWidth, buttonHeight, () =>
        {
            menu.click1.Play();
            menu.SetStateToOptions();
        });
        currentY += buttonHeight + spacing;
        ImGui.SetCursorPos(new Vector2((windowWidth - buttonWidth) / 2, currentY));
        GameMenu.CenterButtonWithBackground("Exit", buttonWidth, buttonHeight, () =>
        {
            menu.click1.Play();
            Window.Instance.Quit();
        });
        ImGui.End();
    }
}

