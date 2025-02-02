using System;
using System.Numerics;
using ImGuiNET;


namespace Spacebox.Game.GUI.Menu
{
    public class NewWorldWindow : MenuWindow
    {
        public GameMenu menu;
        public NewWorldWindow(GameMenu menu)
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
            ImGui.Begin("Create New World", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove
                | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar);
            float inputWidth = windowWidth * 0.8f;
            float inputHeight = windowHeight * 0.06f;
            float buttonWidth = windowWidth * 0.4f;
            float buttonHeight = inputHeight * 1.5f;
            float spacing = windowHeight * 0.02f;
            float labelHeight = ImGui.CalcTextSize("A").Y;
            GameMenu.DrawElementColors(windowPos, new Vector2(windowWidth, windowHeight), windowSize.Y, 0.004f);
            float totalInputHeight = (labelHeight + inputHeight + spacing) * 4;
            float topPadding = (windowHeight - totalInputHeight - buttonHeight - ImGui.GetStyle().WindowPadding.Y * 2) / 2;
            ImGui.Dummy(new Vector2(0, topPadding / 2f));
            menu.CenterInputText("World Name", ref menu.newWorldName, 100, inputWidth, inputHeight);
            ImGui.Dummy(new Vector2(0, spacing));
            menu.CenterInputText("Author", ref menu.newWorldAuthor, 100, inputWidth, inputHeight);
            ImGui.Dummy(new Vector2(0, spacing));
            menu.CenterInputText("Seed", ref menu.newWorldSeed, 100, inputWidth, inputHeight);
            ImGui.Dummy(new Vector2(0, spacing));
            string comboLabel = "Game Mode";
            Vector2 labelSize = ImGui.CalcTextSize(comboLabel);
            ImGui.SetCursorPosX((windowWidth - labelSize.X) * 0.5f);
            ImGui.Text(comboLabel);
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(ImGui.GetStyle().FramePadding.X, (inputHeight - labelHeight) / 2));
            ImGui.SetNextItemWidth(inputWidth);
            ImGui.SetCursorPosX((windowWidth - inputWidth) / 2);
            if (ImGui.BeginCombo("##GameMode", menu.Gamemodes[menu.SelectedGameModeIndex]))
            {
                for (int i = 0; i < menu.Gamemodes.Length; i++)
                {
                    bool isSelected = i == menu.SelectedGameModeIndex;
                    if (ImGui.Selectable(menu.Gamemodes[i], isSelected))
                    {
                        menu.click1.Play();
                        menu.SelectedGameModeIndex = i;
                    }
                    if (isSelected)
                    {
                        ImGui.SetItemDefaultFocus();
                    }
                }
                ImGui.EndCombo();
            }
            ImGui.PopStyleVar();
            string comboLabel2 = "GameSet";
            Vector2 labelSize2 = ImGui.CalcTextSize(comboLabel2);
            ImGui.SetCursorPosX((windowWidth - labelSize2.X) * 0.5f);
            ImGui.Text(comboLabel2);
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(ImGui.GetStyle().FramePadding.X, (inputHeight - labelHeight) / 2));
            ImGui.SetNextItemWidth(inputWidth);
            ImGui.SetCursorPosX((windowWidth - inputWidth) / 2);
            if (ImGui.BeginCombo("##GameSet", menu.GameSets[menu.SelectedGameSetIndex].ModName))
            {
                for (int i = 0; i < menu.GameSets.Count; i++)
                {
                    bool isSelected = i == menu.SelectedGameSetIndex;
                    if (ImGui.Selectable(menu.GameSets[i].ModName, isSelected))
                    {
                        menu.click1.Play();
                        menu.SelectedGameSetIndex = i;
                    }
                    if (isSelected)
                    {
                        ImGui.SetItemDefaultFocus();
                    }
                }
                ImGui.EndCombo();
            }
            ImGui.PopStyleVar();
            float totalButtonWidth = buttonWidth * 2 + spacing;
            float bottomMargin = windowHeight * 0.05f;
            float buttonY = windowHeight - buttonHeight - bottomMargin;
            float buttonStartX = (windowWidth - totalButtonWidth) / 2;
            menu.ButtonWithBackground("Create", new Vector2(buttonWidth, buttonHeight),
                new Vector2(buttonStartX, buttonY),
                () =>
                {
                    menu.click1.Play();
                    if (menu.IsNameUnique(menu.NewWorldName))
                    {
                        menu.CreateNewWorld();
                        menu.SetStateToWorldSelect();
                    }
                });
            menu.ButtonWithBackground("Cancel", new Vector2(buttonWidth, buttonHeight),
                new Vector2(buttonStartX + buttonWidth + spacing, buttonY),
                () =>
                {
                    menu.click1.Play();
                    menu.SetStateToWorldSelect();
                });
            ImGui.End();
        }
    }
}
