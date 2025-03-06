using System;
using System.Numerics;
using ImGuiNET;
using Engine;
using Engine.Audio;

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
            float windowWidth = windowSize.X * 0.3f;
            float windowHeight = windowSize.Y * 0.4f;
            Vector2 windowPos = GameMenu.CenterNextWindow2(windowWidth, windowHeight);
            ImGui.SetNextWindowPos(windowPos);
            ImGui.SetNextWindowSize(new Vector2(windowWidth, windowHeight));
            ImGui.Begin("Options", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove
                | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar);
            float buttonWidth = windowWidth * 0.5f;
            float buttonHeight = windowHeight * 0.1f;
            GameMenu.DrawElementColors(windowPos, new Vector2(windowWidth, windowHeight), windowSize.Y, 0.005f);
            ImGui.SetCursorPosY(windowHeight - buttonHeight - ImGui.GetStyle().WindowPadding.Y * 2);
            ImGui.SetCursorPosX((windowWidth - buttonWidth) / 2);
            if (ImGui.Button("Back", new Vector2(buttonWidth, buttonHeight)))
            {
                menu.Click1.Play();
                menu.SetStateToMain();
            }
            ImGui.End();
        }
    }
}
