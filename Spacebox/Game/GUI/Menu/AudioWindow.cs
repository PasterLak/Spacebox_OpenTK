using ImGuiNET;
using System.Numerics;
using static Spacebox.Game.GUI.Menu.ControlsWindow;

namespace Spacebox.Game.GUI.Menu
{
    public class AudioWindow : MenuWindow
    {
        private GameMenu menu;

        private int _master = 100;
        private int _ambient = 80;
        private int _music = 80;
        private int _effects = 90;
        private int _ui = 75;
        private bool _muteWhenUnfocused = false;
        private bool _menuMusic = true;

        public AudioWindow(GameMenu menu)
        {
            this.menu = menu;
        }

        public override void Render()
        {
            var settings = Settings.Audio;

            _master = settings.Master;
            _ambient = settings.Ambient;  
            _music = settings.Music;
            _effects = settings.Effects;
            _ui = settings.UI;
            _muteWhenUnfocused = settings.MuteWhenUnfocused;
            _menuMusic = settings.MenuMusic;


            SettingsUI.Render("Audio", "Audio", menu, 5,
                (listSize, rowH) =>
                {
                    ImGui.BeginTable("table##audio", 2, ImGuiTableFlags.NoBordersInBody);
                    float totalW = listSize.X;
                    ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, totalW * 0.7f);
                    ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthFixed, totalW * 0.3f);

                    Vector2 dummyOffset = new Vector2(totalW * 0.3f / 1.4f, 0);

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text("Master Volume");
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(totalW * 0.28f);
                    ImGui.SliderInt("##master", ref _master, 0, 100);

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text("Ambient Volume");
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(totalW * 0.28f);
                    ImGui.SliderInt("##ambient", ref _ambient, 0, 100 );

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text("Music Volume");
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(totalW * 0.28f);
                    ImGui.SliderInt("##music", ref _music, 0, 100);

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text("Effects Volume");
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(totalW * 0.28f);
                    ImGui.SliderInt("##effects", ref _effects, 0, 100);

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text("UI Volume");
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(totalW * 0.28f);
                    ImGui.SliderInt("##ui", ref _ui, 0, 100);

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text("Mute When Unfocused");
                    ImGui.TableNextColumn();
                    ImGui.Dummy(dummyOffset); ImGui.SameLine();
                    ImGui.Checkbox("##muteUnfocused", ref _muteWhenUnfocused);

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text("Menu Music");
                    ImGui.TableNextColumn();
                    ImGui.Dummy(dummyOffset); ImGui.SameLine();
                    ImGui.Checkbox("##menuMusic", ref _menuMusic);

                    ImGui.EndTable();
                },
                () => { menu.Click1.Play(); menu.SetStateToOptions();
                    SettingsService.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "/Resources/Settings.json"), Settings.AsGameSettings());
                },
                () => { menu.Click1.Play(); menu.SetStateToOptions(); }
            );

            settings.Master = _master;
            settings.Ambient = _ambient;
            settings.Music = _music;
            settings.Effects = _effects;
            settings.UI = _ui;
            settings.MuteWhenUnfocused = _muteWhenUnfocused;
            settings.MenuMusic = _menuMusic;

           


        }
    }
}
