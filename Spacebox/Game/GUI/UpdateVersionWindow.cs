using System;
using System.Numerics;
using ImGuiNET;
using Engine;
using Engine.Audio;
using Spacebox.Game.GUI.Menu;

namespace Spacebox.Game.GUI
{
    public class UpdateVersionWindow : MenuWindow
    {
        private GameMenu menu;
        public UpdateVersionWindow(GameMenu menu)
        {
            this.menu = menu;
        }
        public override void Render()
        {
            Vector2 displaySize = ImGui.GetIO().DisplaySize;
            float windowWidth = displaySize.Y * 0.5f;
            float windowHeight = displaySize.Y * 0.12f;
            Vector2 windowPos = GameMenu.CenterNextWindow2(windowWidth, windowHeight);
            ImGui.SetNextWindowPos(windowPos);
            ImGui.SetNextWindowSize(new Vector2(windowWidth, windowHeight));
            ImGui.Begin("UpdateVersionWindow", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove
                | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar);
            float buttonWidth = windowWidth * 0.8f;
            float buttonHeight = windowHeight * 0.15f;
            float spacing = windowHeight * 0.03f;
            GameMenu.DrawElementColors(windowPos, new Vector2(windowWidth, windowHeight), displaySize.Y, 0.005f);
            float totalButtonsHeight = buttonHeight * 3 + spacing * 2;
            string text = "This map is made on an older version of the game";
            var textSize = ImGui.CalcTextSize(text);
            ImGui.SetCursorPos(new Vector2(windowWidth * 0.5f - textSize.X * 0.5f, spacing));
            ImGui.Text(text);
            string text2 = "Do you want to convert the map?";
            var textSize2 = ImGui.CalcTextSize(text2);
            ImGui.SetCursorPos(new Vector2(windowWidth * 0.5f - textSize2.X * 0.5f, spacing * 2 + textSize.Y));
            ImGui.Text(text2);
            ImGui.Dummy(new Vector2(0, (windowHeight - totalButtonsHeight) / 4));
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.8f, 0.25f, 0.25f, 1.0f));
            GameMenu.CenterButtonWithBackground("Yes, convert", buttonWidth, buttonHeight, () =>
            {
                menu.click1.Play();
                if (VersionConverter.Convert(menu.selectedWorld, Application.Version))
                {
                    menu.selectedWorld.LastEditDate = DateTime.Now.ToString();
                    WorldInfoSaver.Save(menu.selectedWorld);
                }
                menu.showVersionConvertWindow = false;
            });
            ImGui.PopStyleColor(1);
            ImGui.Dummy(new Vector2(0, spacing));
            GameMenu.CenterButtonWithBackground("No", buttonWidth, buttonHeight, () =>
            {
                menu.showVersionConvertWindow = false;
                menu.click1.Play();
            });
            ImGui.Dummy(new Vector2(0, spacing));
            ImGui.End();
        }
    }
}
