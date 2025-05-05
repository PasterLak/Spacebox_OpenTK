
using System.Numerics;
using ImGuiNET;


namespace Spacebox.Game.GUI.Menu
{
    public class OptionsWindow : MenuWindow
    {
        private GameMenu menu;
        public OptionsWindow(GameMenu menu)
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
            int buttonCount = 5;
            float spacing = (windowHeight - (buttonCount * buttonHeight)) / (buttonCount + 1);
            float currentY = spacing;
            ImGui.SetCursorPos(new Vector2((windowWidth - buttonWidth) / 2, currentY));
            GameMenu.CenterButtonWithBackground("Audio", buttonWidth, buttonHeight, () =>
            {
                menu.Click1.Play();
               
            });
            currentY += buttonHeight + spacing;
            ImGui.SetCursorPos(new Vector2((windowWidth - buttonWidth) / 2, currentY));
            GameMenu.CenterButtonWithBackground("Game", buttonWidth, buttonHeight, () =>
            {
                menu.Click1.Play();
               
            });
            currentY += buttonHeight + spacing;
            ImGui.SetCursorPos(new Vector2((windowWidth - buttonWidth) / 2, currentY));
            GameMenu.CenterButtonWithBackground("Graphics", buttonWidth, buttonHeight, () =>
            {
                menu.Click1.Play();
               
            });
            currentY += buttonHeight + spacing;
            ImGui.SetCursorPos(new Vector2((windowWidth - buttonWidth) / 2, currentY));
            GameMenu.CenterButtonWithBackground("Controls", buttonWidth, buttonHeight, () =>
            {
                menu.Click1.Play();
                menu.SetStateToSettingsControls();
            });
            currentY += buttonHeight + spacing;
            ImGui.SetCursorPos(new Vector2((windowWidth - buttonWidth) / 2, currentY));
            GameMenu.CenterButtonWithBackground("Back", buttonWidth, buttonHeight, () =>
            {
                menu.Click1.Play();
                menu.SetStateToMain();
            });
            ImGui.End();
        }
    }
}
