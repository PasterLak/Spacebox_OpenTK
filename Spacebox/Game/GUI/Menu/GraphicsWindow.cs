using Engine;
using Engine.Light;
using ImGuiNET;
using Spacebox.Game.Generation;
using System.Numerics;
using static Spacebox.Game.GUI.Menu.ControlsWindow;

namespace Spacebox.Game.GUI.Menu
{
    public class GraphicsWindow : MenuWindow
    {
        private GameMenu menu;

        private bool _vsync = true;
        private bool _ao = true;
        private bool _voxelLighting = true;
        private bool _postProcessing = true;
        private bool _shadows = true;
        private bool _enableEffects = false;
        private int _fov = 75;
        private int _resolution = 100;
        private readonly string[] _modes = { "Fullscreen", "Borderless", "Windowed" };
        private int _modeIndex = 2;

        public GraphicsWindow(GameMenu menu)
        {
            this.menu = menu;
        }

    
        public override void Render()
        {

            _vsync =Settings.Graphics.VSync;
            _ao = Settings.Graphics.AO;
            _voxelLighting = Settings.Graphics.VoxelLighting;
            _postProcessing = Settings.Graphics.PostProcessing;
            _shadows =Settings.Graphics.Shadows;
            _enableEffects = Settings.Graphics.EffectsEnabled;
            _fov = Settings.Graphics.Fov;
            _resolution = Settings.Graphics.ResolutionScalePercent;
            _modeIndex = (int)Settings.Graphics.WindowMode;
         

            SettingsUI.Render("Graphics", "Graphics", menu, 5,
                (listSize, rowH) =>
                {
                    ImGui.BeginTable("table##graphics", 2, ImGuiTableFlags.NoBordersInBody);
                    float totalW = listSize.X;
                    ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, totalW * 0.7f);
                    ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthFixed, totalW * 0.3f);

                    var dummyOffset = new Vector2(totalW * 0.3f / 1.4f, 0);

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text("Window Mode");
                    
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(totalW * 0.28f);
                    ImGui.Combo("##winmode", ref _modeIndex, _modes, _modes.Length);
                    UIHelper.ShowTooltip("Switch between fullscreen, windowed, or borderless display modes");

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();

                    ImGui.Text("VSync");
                  
                    ImGui.TableNextColumn();
                    ImGui.Dummy(dummyOffset); ImGui.SameLine();
                    ImGui.Checkbox("##vsync", ref _vsync);
                    UIHelper.ShowTooltip("Synchronizes frame rate with monitor refresh rate to reduce screen tearing");

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();

                    ImGui.Text("Ambient Occlusion");

                    ImGui.TableNextColumn();
                    ImGui.Dummy(dummyOffset); ImGui.SameLine();
                    ImGui.Checkbox("##ao", ref _ao);
                    UIHelper.ShowTooltip("Darkening the corners between blocks");

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();

                    ImGui.Text("Voxel Lighting");

                    ImGui.TableNextColumn();
                    ImGui.Dummy(dummyOffset); ImGui.SameLine();
                    ImGui.Checkbox("##voxel_lighting", ref _voxelLighting);
                    UIHelper.ShowTooltip("Calculate lighting for glowing blocks");


                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text("Post Processing");
                    
                
                    ImGui.TableNextColumn();
                    ImGui.Dummy(dummyOffset); ImGui.SameLine();
                    ImGui.Checkbox("##bloom", ref _postProcessing );
                    UIHelper.ShowTooltip("Applies visual enhancements like bloom, color grading, and depth of field");

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text("Shadows");
                    ImGui.TableNextColumn();
                    ImGui.Dummy(dummyOffset); ImGui.SameLine();
                    ImGui.Checkbox("##shadows", ref _shadows);

                    ImGui.TableNextRow(); 
                    ImGui.TableNextColumn();
                    ImGui.Text("Enable Effects");
                    ImGui.TableNextColumn();
                    ImGui.Dummy(dummyOffset); ImGui.SameLine();
                   
                    ImGui.Checkbox("##mblur", ref _enableEffects);
                    UIHelper.ShowTooltip("Toggles visual effects such as particles, dust");

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text("FOV");
                  
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(totalW * 0.28f);
                    ImGui.SliderInt("##fov", ref _fov, 50, 120);
                    UIHelper.ShowTooltip("Adjusts your field of view — higher values let you see more of the scene");

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text("Resolution");
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(totalW * 0.28f);
                    ImGui.SliderInt("##resolution", ref _resolution, 10, 100);

                    ImGui.EndTable();
                },
                () => { menu.Click1.Play(); menu.SetStateToOptions();
                    SettingsService.Save(Settings.AsGameSettings());


                },
                () => { menu.Click1.Play(); menu.SetStateToOptions();
                    SettingsService.Save(Settings.AsGameSettings());
                }
            );

            Settings.Graphics.VSync = _vsync;
            Settings.Graphics.AO = _ao;
            MeshGenerator.EnableAO = _ao;
            Settings.Graphics.VoxelLighting = _voxelLighting;
            LightManager.EnableLighting = _voxelLighting;
            Settings.Graphics.PostProcessing = _postProcessing;
            Settings.Graphics.Shadows = _shadows;
            Settings.Graphics.EffectsEnabled = _enableEffects;
            Settings.Graphics.Fov = _fov;
            Settings.Graphics.ResolutionScalePercent = _resolution;
          
            if(_modes[_modeIndex] == "Fullscreen")
            {
                Settings.Graphics.WindowMode = WindowMode.Fullscreen;
            }
            else if(_modes[_modeIndex] == "Borderless")
            {
                Settings.Graphics.WindowMode = WindowMode.Borderless;
            }
            else
            {
                Settings.Graphics.WindowMode = WindowMode.Windowed;
            }
        }
    }
}
