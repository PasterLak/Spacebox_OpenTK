
using System.Numerics;
using ImGuiNET;


namespace Spacebox.Game.GUI.Menu
{
    public class WorldSelectWindow : MenuWindow
    {
        public GameMenu menu;
        public WorldSelectWindow(GameMenu menu)
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
            ImGui.Begin("Select World", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove
                | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar);
            float horizontalMargin = windowWidth * 0.06f;
            float verticalSpacing = windowHeight * 0.03f;
            float listHeight = windowHeight * 0.4f;
            float infoHeight = windowHeight * 0.25f;
            GameMenu.DrawElementColors(windowPos, new Vector2(windowWidth, windowHeight), windowSize.Y, 0.004f);
            float contentWidth = windowWidth - horizontalMargin * 2;
            ImGui.Dummy(new Vector2(0, verticalSpacing));
            ImGui.SetCursorPosX(horizontalMargin);
            ImGui.BeginChild("WorldList", new Vector2(contentWidth, listHeight));
            for (int i = 0; i < menu.Worlds.Count; i++)
            {
                var world = menu.Worlds[i];
                bool isSelected = menu.selectedWorld == world;
                if (ImGui.Selectable(" " + world.Name + " ", isSelected))
                {
                    menu.click1.Play();
                    menu.selectedWorld = world;
                }
            }
            ImGui.EndChild();
            ImGui.Dummy(new Vector2(0, verticalSpacing));
            ImGui.SetCursorPosX(horizontalMargin);
            ImGui.BeginChild("WorldInfo", new Vector2(contentWidth, infoHeight));
            GameMenu.DrawElementColors(ImGui.GetCursorPos(), new Vector2(contentWidth, infoHeight), windowSize.Y, 0.004f);
            if (menu.selectedWorld != null)
            {
                ImGui.Text(" Name: " + menu.selectedWorld.Name);
                ImGui.Text(" Author: " + menu.selectedWorld.Author);
                ImGui.Text(" Mod: " + menu.GetModNameById(menu.selectedWorld.ModId));
                if (menu.selectedWorld.GameVersion != Application.Version)
                {
                    ImGui.TextColored(new Vector4(1, 0, 0, 1), " Version: " + menu.selectedWorld.GameVersion);
                }
                else
                {
                    ImGui.Text(" Version: " + menu.selectedWorld.GameVersion);
                }
            }
            ImGui.EndChild();
            ImGui.Dummy(new Vector2(0, verticalSpacing));
            if (menu.selectedWorld != null)
            {
                float buttonSpacing = windowWidth * 0.02f;
                float buttonWidth = (contentWidth - buttonSpacing) / 2;
                float buttonHeight = windowHeight * 0.08f;
                float buttonY = listHeight + infoHeight + verticalSpacing * 3.5f;
                float buttonStartX = horizontalMargin + (contentWidth - (buttonWidth * 2 + buttonSpacing)) / 2;
                menu.ButtonWithBackground("Play", new Vector2(buttonWidth, buttonHeight),
                    new Vector2(buttonStartX, buttonY + windowHeight * 0.02f),
                    () =>
                    {
                        menu.click1.Play();
                        if (VersionConverter.IsVersionOld(menu.selectedWorld.GameVersion, Application.Version))
                        {
                            menu.showVersionConvertWindow = true;
                        }
                        else
                        {
                            menu.LoadWorld(menu.selectedWorld);
                        }
                    });
                menu.ButtonWithBackground("Edit", new Vector2(buttonWidth - buttonHeight - buttonSpacing, buttonHeight),
                    new Vector2(buttonStartX + buttonWidth + buttonSpacing, buttonY + windowHeight * 0.02f),
                    () =>
                    {
                        menu.click1.Play();
                        // Функционал редактирования
                    });
                menu.ButtonWithBackgroundAndIcon("", new Vector2(buttonHeight, buttonHeight),
                    new Vector2(buttonStartX + buttonWidth + buttonSpacing + buttonWidth - buttonHeight, buttonY + windowHeight * 0.02f),
                    () =>
                    {
                        menu.click1.Play();
                        menu.showDeleteWindow = true;
                    });
            }
            ImGui.Dummy(new Vector2(0, verticalSpacing));
            float bottomButtonSpacing = windowWidth * 0.02f;
            float bottomButtonWidth = (contentWidth - bottomButtonSpacing) / 2;
            float bottomButtonHeight = windowHeight * 0.08f;
            float bottomMargin = windowHeight * 0.05f;
            float bottomButtonY = windowHeight - bottomButtonHeight - bottomMargin;
            float bottomButtonStartX = horizontalMargin + (contentWidth - (bottomButtonWidth * 2 + bottomButtonSpacing)) / 2;
            menu.ButtonWithBackground("Create New World", new Vector2(bottomButtonWidth, bottomButtonHeight),
                new Vector2(bottomButtonStartX, bottomButtonY),
                () =>
                {
                    menu.click1.Play();
                    menu.GenerateRandomSeedAndName();
                    menu.SetStateToNewWorld();
                });
            menu.ButtonWithBackground("Back", new Vector2(bottomButtonWidth, bottomButtonHeight),
                new Vector2(bottomButtonStartX + bottomButtonWidth + bottomButtonSpacing, bottomButtonY),
                () =>
                {
                    menu.click1.Play();
                    menu.SetStateToMain();
                });
            ImGui.End();
        }
    }
}
