
using System.Numerics;
using ImGuiNET;


namespace Spacebox.Game.GUI.Menu
{
    public class GraphicsWindow : MenuWindow
    {
        private GameMenu menu;

        private Dictionary<string, string> controls = new Dictionary<string, string>()
        {
            { "move forward", "W"},
            { "move backward", "S"},
            { "move left", "A"},
            { "move right", "D"},
            { "open inventory", "Tab"},
            { "flashlight", "F"},
            { "speed up", "Shift"},

        };

        public GraphicsWindow(GameMenu menu)
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
            ImGui.Begin("Controls", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove
                | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar);
            float buttonWidth = windowWidth * 0.9f;
            float buttonHeight = windowHeight * 0.12f;
            GameMenu.DrawElementColors(windowPos, new Vector2(windowWidth, windowHeight), windowSize.Y, 0.005f);
            int buttonCount = 5;
            float spacing = (windowHeight - (buttonCount * buttonHeight)) / (buttonCount + 1);
            float currentY = spacing;

            var listSize = new Vector2(buttonWidth, windowHeight - buttonHeight - spacing * 3);

            ImGui.SetCursorPos(new Vector2((windowWidth - buttonWidth) / 2, spacing));
            ImGui.BeginChild("list", listSize);

            ImGui.Text("Change controls");
            ImGui.Dummy(new Vector2(buttonWidth, spacing / 2f));
            ImGui.Text("Action             Key");

            ImGui.BeginTable("table", 2, ImGuiTableFlags.NoBordersInBody);


            float totalW = listSize.X;
            ImGui.TableSetupColumn("Key", ImGuiTableColumnFlags.WidthFixed, totalW * 0.7f);
            ImGui.TableSetupColumn("Action", ImGuiTableColumnFlags.WidthFixed, totalW * 0.3f);

            foreach (var kv in controls)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Button(kv.Key, new Vector2(totalW * 0.7f, listSize.Y / 6f));
                ImGui.TableNextColumn();
                ImGui.Button(kv.Value, new Vector2(totalW * 0.3f, listSize.Y / 6f));
            }


            ImGui.EndTable();

            ImGui.EndChild();


            ImGui.SetCursorPos(new Vector2((windowWidth - buttonWidth) / 2, windowHeight - buttonHeight - spacing));
            menu.ButtonWithBackground("Save", new Vector2(listSize.X / 2f - spacing, buttonHeight), new Vector2(spacing, windowHeight - buttonHeight - spacing), () =>
            {
                menu.Click1.Play();
                menu.SetStateToOptions();
            });
            // ImGui.SameLine();
            menu.ButtonWithBackground("Back", new Vector2(listSize.X / 2f - spacing, buttonHeight), new Vector2(windowWidth - listSize.X / 2f, windowHeight - buttonHeight - spacing), () =>
            {
                menu.Click1.Play();
                menu.SetStateToOptions();
            });
            ImGui.End();
        }
    }
}
