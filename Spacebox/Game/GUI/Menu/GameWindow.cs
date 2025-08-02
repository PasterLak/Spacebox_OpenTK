using ImGuiNET;
using System.Numerics;
using static Spacebox.Game.GUI.Menu.ControlsWindow;

namespace Spacebox.Game.GUI.Menu
{
    public class GameWindow : MenuWindow
    {
        private GameMenu menu;

        private int _drawDistance = 12;
        private readonly string[] _languages = { "English" };
        private int _languageIndex = 0;
        private bool _keepInventory = false;

        public GameWindow(GameMenu menu)
        {
            this.menu = menu;
        }

        public override void Render()
        {

            _drawDistance = Settings.Gameplay.DrawDistance;
            _languageIndex = Settings.Gameplay.Language == "English" ? 0 : 0; // Currently only English is supported

            SettingsUI.Render("Game", "Game", menu, 5,
                (listSize, rowH) =>
                {
                    ImGui.BeginTable("table##game", 2, ImGuiTableFlags.NoBordersInBody);
                    float totalW = listSize.X;
                    ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, totalW * 0.7f);
                    ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthFixed, totalW * 0.3f);

                    Vector2 dummyOffset = new Vector2(totalW * 0.3f / 1.4f, 0);

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text("Draw Distance");
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(totalW * 0.28f);
                    ImGui.SliderInt("##drawdistance", ref _drawDistance, 2, 32);

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text("Language");
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(totalW * 0.28f);
                    ImGui.Combo("##language", ref _languageIndex, _languages, _languages.Length);

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text("Keep Inventory");
                    ImGui.TableNextColumn();
                    ImGui.Dummy(dummyOffset); ImGui.SameLine();
                    ImGui.Checkbox("##keepinv", ref _keepInventory);

                    ImGui.EndTable();
                },
                () => { menu.Click1.Play(); menu.SetStateToOptions();
                    SettingsService.Save(Settings.AsGameSettings());
                },
                () => { menu.Click1.Play(); menu.SetStateToOptions(); }
            );

            Settings.Gameplay.DrawDistance = _drawDistance;
            Settings.Gameplay.Language = _languages[_languageIndex];
        }
    }
}
