
using System.Numerics;
using ImGuiNET;


namespace Spacebox.Game.GUI.Menu
{
    public class ControlsWindow : MenuWindow
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

        public ControlsWindow(GameMenu menu)
        {
            this.menu = menu;
        }
        public override void Render()
        {
            SettingsUI.Render("Controls", "Controls", menu, 5,
                (listSize, rowH) =>
                {
                    ImGui.Text("Action             Key");
                    ImGui.Dummy(new Vector2(listSize.X, rowH * 0.25f));

                    ImGui.BeginTable("table##controls", 2, ImGuiTableFlags.NoBordersInBody);
                    float totalW = listSize.X;
                    ImGui.TableSetupColumn("Action", ImGuiTableColumnFlags.WidthFixed, totalW * 0.7f);
                    ImGui.TableSetupColumn("Key", ImGuiTableColumnFlags.WidthFixed, totalW * 0.3f);

                    foreach (var kv in controls)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.Button(kv.Key, new Vector2(totalW * 0.7f, rowH));
                        ImGui.TableNextColumn();
                        ImGui.Button(kv.Value, new Vector2(totalW * 0.3f, rowH));
                    }
                    ImGui.EndTable();
                },
                () => { menu.Click1.Play(); menu.SetStateToOptions(); },
                () => { menu.Click1.Play(); menu.SetStateToOptions(); }
            );
        }


        public static class SettingsUI
        {
            public static void Render(string windowId, string header, GameMenu menu, int buttonCount, Action<Vector2, float> drawContent, Action onSave, Action onBack)
            {
                var io = ImGui.GetIO();
                float ww = io.DisplaySize.X * 0.25f;
                float wh = io.DisplaySize.Y * 0.3f;
                var pos = GameMenu.CenterNextWindow2(ww, wh);

                ImGui.SetNextWindowPos(pos);
                ImGui.SetNextWindowSize(new Vector2(ww, wh));
                ImGui.Begin(windowId, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar);

                GameMenu.DrawElementColors(pos, new Vector2(ww, wh), io.DisplaySize.Y, 0.005f);

                float btnW = ww * 0.9f;
                float btnH = wh * 0.12f;
                float spacing = (wh - (buttonCount * btnH)) / (buttonCount + 1);

                ImGui.SetCursorPos(new Vector2((ww - btnW) / 2f, spacing));
                var textSize = ImGui.CalcTextSize(header);
                ImGui.SetCursorPos(new Vector2((ww - textSize.X) / 2f, spacing));
                ImGui.Text(header);

                float headerBlockH = ImGui.GetTextLineHeightWithSpacing() * 1.2f;
                var listSize = new Vector2(btnW, wh - btnH - spacing * 3 - headerBlockH);

                ImGui.SetCursorPos(new Vector2((ww - btnW) / 2f, spacing + headerBlockH));
                ImGui.BeginChild($"list##{windowId}", listSize);

                float rowH = listSize.Y / 6f;
                drawContent(listSize, rowH);

                ImGui.EndChild();

                ImGui.SetCursorPos(new Vector2((ww - btnW) / 2f, wh - btnH - spacing));
                menu.ButtonWithBackground("Save", new Vector2(listSize.X / 2f - spacing, btnH), new Vector2(spacing, wh - btnH - spacing), onSave);
                menu.ButtonWithBackground("Back", new Vector2(listSize.X / 2f - spacing, btnH), new Vector2(ww - listSize.X / 2f, wh - btnH - spacing), onBack);

                ImGui.End();
            }
        }


    }
}
