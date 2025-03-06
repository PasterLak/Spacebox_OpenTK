using System;
using System.Numerics;
using ImGuiNET;
using Engine;
using Engine.Audio;

namespace Spacebox.Game.GUI.Menu
{
    public class DeleteWindow : MenuWindow
    {
        private GameMenu menu;
        public DeleteWindow(GameMenu menu)
        {
            this.menu = menu;
        }
        public override void Render()
        {
            Vector2 displaySize = ImGui.GetIO().DisplaySize;
            float windowWidth = displaySize.Y * 0.15f;
            float windowHeight = displaySize.Y * 0.15f;
            Vector2 windowPos = GameMenu.CenterNextWindow2(windowWidth, windowHeight);
            ImGui.SetNextWindowPos(windowPos);
            ImGui.SetNextWindowSize(new Vector2(windowWidth, windowHeight));
            ImGui.Begin("Delete", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove
                | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar);
            float buttonWidth = windowWidth * 0.8f;
            float buttonHeight = windowHeight * 0.12f;
            float spacing = windowHeight * 0.03f;
            GameMenu.DrawElementColors(windowPos, new Vector2(windowWidth, windowHeight), displaySize.Y, 0.005f);
            float totalButtonsHeight = buttonHeight * 3 + spacing * 2;
            var textSize = ImGui.CalcTextSize("Are you sure?");
            ImGui.SetCursorPos(new Vector2(windowWidth * 0.5f - textSize.X * 0.5f, spacing));
            ImGui.Text("Are you sure?");
            ImGui.Dummy(new Vector2(0, (windowHeight - totalButtonsHeight) / 4));
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.8f, 0.25f, 0.25f, 1.0f));
            GameMenu.CenterButtonWithBackground("Yes, delete", buttonWidth, buttonHeight, () =>
            {
                menu.Click1.Play();
                menu.showDeleteWindow = false;
                menu.DeleteWorld(menu.selectedWorld);
                menu.selectedWorld = null;
                if (menu.Worlds.Count > 0)
                {
                    menu.selectedWorld = menu.Worlds[0];
                }
            });
            ImGui.PopStyleColor(1);
            ImGui.Dummy(new Vector2(0, spacing));
            GameMenu.CenterButtonWithBackground("No", buttonWidth, buttonHeight, () =>
            {
                menu.showDeleteWindow = false;
                menu.Click1.Play();
            });
            ImGui.Dummy(new Vector2(0, spacing));
            ImGui.End();
        }
    }
}
