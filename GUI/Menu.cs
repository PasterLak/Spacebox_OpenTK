// Menu.cs
using ImGuiNET;

using Spacebox.Common;
using Spacebox.Common.SceneManagment;
using Spacebox.Extensions;
using Spacebox.Scenes;
using System.Numerics;

namespace Spacebox
{
    public class Menu
    {
        public bool IsVisible { get; set; } = true;

        
        public void Toggle()
        {
            IsVisible = !IsVisible;
        }

        public void OnGUI()
        {
            if (!IsVisible)
                return;

            Time.StartOnGUI();

            var io = ImGui.GetIO();
            Vector2 displaySize = new Vector2(io.DisplaySize.X, io.DisplaySize.Y);

            float windowWidthRatio = 0.3f;
            float windowHeightRatio = 0.4f;

            Vector2 windowSize = new OpenTK.Mathematics.Vector2(
                displaySize.X * windowWidthRatio,
                displaySize.Y * windowHeightRatio
            ).ToSystemVector2();

            Vector2 windowPos = new OpenTK.Mathematics.Vector2(
                (displaySize.X - windowSize.X) / 2,
                (displaySize.Y - windowSize.Y) / 2
            ).ToSystemVector2();

            ImGui.SetNextWindowSize(windowSize, ImGuiCond.Always);
            ImGui.SetNextWindowPos(windowPos, ImGuiCond.Always);
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0, 0, 0, 0.7f));

            ImGui.Begin("Menu", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse);

            Vector2 currentWindowSize = new Vector2(ImGui.GetWindowSize().X, ImGui.GetWindowSize().Y);

            float buttonWidth = currentWindowSize.X * 0.95f;
            float buttonHeight = currentWindowSize.Y * 0.2f;

            float xOffset = (currentWindowSize.X - buttonWidth) / 2;
            float yOffset = (currentWindowSize.Y - buttonHeight) / 2;

            ImGui.SetCursorPosX(xOffset); 
            if (ImGui.Button("Load Game Scene", new System.Numerics.Vector2(buttonWidth, buttonHeight)))
            {
                SceneManager.LoadScene(typeof(GameScene));
            }

            ImGui.Spacing();

            ImGui.SetCursorPosX(xOffset);
            if (ImGui.Button("Load Space Scene", new System.Numerics.Vector2(buttonWidth, buttonHeight)))
            {
                SceneManager.LoadScene(typeof(SpaceScene));
            }

            ImGui.Spacing();

            ImGui.SetCursorPosX(xOffset); 
            if (ImGui.Button("Settings", new System.Numerics.Vector2(buttonWidth, buttonHeight)))
            {
                // Settings action
            }

            ImGui.Spacing();

            ImGui.SetCursorPosX(xOffset);
            if (ImGui.Button("Exit", new System.Numerics.Vector2(buttonWidth, buttonHeight)))
            {
                Environment.Exit(0);
            }

            

            ImGui.End();
            ImGui.PopStyleColor();

            Time.EndOnGUI();

            ImGui.ShowStyleEditor();
        }
    }
}
