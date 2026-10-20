
using System.Numerics;
using Engine;
using ImGuiNET;
using Spacebox.Game.GUI.Menu;
using Spacebox.Game.Versioning;

namespace Spacebox.Game.GUI
{
    public class UpdateVersionWindow : MenuWindow
    {
        private static readonly Vector4 ErrorColor = new Vector4(1f, 0.4f, 0.35f, 1f);
        private static readonly Vector4 WarningColor = new Vector4(1f, 0.75f, 0.3f, 1f);

        private GameMenu menu;
        private string _error;
        private WorldUpgradeResult _result;
        private WorldInfo _checkedWorld;
        private WorldUpgradeStatus _status;
        private string _reason;

        public UpdateVersionWindow(GameMenu menu)
        {
            this.menu = menu;
        }

        public void Reset()
        {
            _error = null;
            _result = null;
            _checkedWorld = null;
            _reason = null;
        }

        private void EnsureChecked(WorldInfo world)
        {
            if (_checkedWorld == world) return;

            _checkedWorld = world;
            _status = VersionConverter.CheckWorld(world, out _reason);
        }

        public override void Render()
        {
            var world = menu.selectedWorld;
            if (world == null)
            {
                Close();
                return;
            }

            Vector2 displaySize = ImGui.GetIO().DisplaySize;
            float windowWidth = displaySize.Y * 0.6f;
            float windowHeight = displaySize.Y * 0.3f;
            Vector2 windowPos = GameMenu.CenterNextWindow2(windowWidth, windowHeight);
            ImGui.SetNextWindowPos(windowPos);
            ImGui.SetNextWindowSize(new Vector2(windowWidth, windowHeight));
            ImGui.Begin("UpdateVersionWindow", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove
                | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar);
            GameMenu.DrawElementColors(windowPos, new Vector2(windowWidth, windowHeight), displaySize.Y, 0.005f);

            float padding = windowWidth * 0.05f;
            float buttonWidth = windowWidth * 0.7f;
            float buttonHeight = windowHeight * 0.12f;
            ImGui.SetCursorPos(new Vector2(padding, padding * 0.6f));
            ImGui.PushTextWrapPos(windowWidth - padding);

            EnsureChecked(world);
            if (_result != null) RenderConverted(world, padding);
            else if (_status == WorldUpgradeStatus.Ok) RenderConvertible(world, padding);
            else RenderUnsupported(world, _reason, padding);

            ImGui.PopTextWrapPos();
            RenderButtons(world, windowHeight, buttonWidth, buttonHeight, padding);
            ImGui.End();
        }

        private void RenderConvertible(WorldInfo world, float padding)
        {
            ImGui.Text($"\"{world.Name}\" was made with version {world.GameVersion}.");
            ImGui.SetCursorPosX(padding);
            ImGui.Text($"Convert it to version {Application.Version}?");
            ImGui.Spacing();

            foreach (var step in VersionConverter.GetSteps(world.GameVersion, Application.Version))
            {
                ImGui.SetCursorPosX(padding);
                ImGui.TextDisabled("- " + step.Description);
            }

            ImGui.Spacing();
            ImGui.SetCursorPosX(padding);
            ImGui.TextDisabled("A backup is saved in Worlds/_backups first.");

            if (string.IsNullOrEmpty(_error)) return;
            ImGui.SetCursorPosX(padding);
            ImGui.TextColored(ErrorColor, _error);
        }

        private void RenderConverted(WorldInfo world, float padding)
        {
            ImGui.Text($"\"{world.Name}\" was converted to version {_result.ToVersion}.");
            ImGui.SetCursorPosX(padding);
            ImGui.TextDisabled(_result.Summary);

            foreach (var warning in _result.Warnings)
            {
                ImGui.SetCursorPosX(padding);
                ImGui.TextColored(WarningColor, warning);
            }

            if (string.IsNullOrEmpty(_result.BackupPath)) return;
            ImGui.SetCursorPosX(padding);
            ImGui.TextDisabled("Backup: " + Path.GetFileName(_result.BackupPath));
        }

        private static void RenderUnsupported(WorldInfo world, string reason, float padding)
        {
            ImGui.Text($"\"{world.Name}\" cannot be opened.");
            ImGui.SetCursorPosX(padding);
            ImGui.TextColored(ErrorColor, reason ?? "Unknown version");
        }

        private void RenderButtons(WorldInfo world, float windowHeight, float buttonWidth, float buttonHeight, float padding)
        {
            float y = windowHeight - (buttonHeight + padding * 0.4f) * 2f - padding * 0.3f;
            bool convertible = _result == null && _status == WorldUpgradeStatus.Ok;

            if (_result != null)
            {
                ImGui.SetCursorPosY(y);
                GameMenu.CenterButtonWithBackground("Play", buttonWidth, buttonHeight, () =>
                {
                    menu.Click1.Play();
                    Close();
                    menu.LoadWorld(world);
                });
            }
            else if (convertible)
            {
                ImGui.SetCursorPosY(y);
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.8f, 0.25f, 0.25f, 1.0f));
                GameMenu.CenterButtonWithBackground("Yes, convert", buttonWidth, buttonHeight, () => Convert(world));
                ImGui.PopStyleColor(1);
            }

            ImGui.SetCursorPosY(y + buttonHeight + padding * 0.4f);
            GameMenu.CenterButtonWithBackground(convertible ? "No" : "Back", buttonWidth, buttonHeight, () =>
            {
                menu.Click1.Play();
                Close();
            });
        }

        private void Convert(WorldInfo world)
        {
            menu.Click1.Play();
            if (!VersionConverter.Convert(world, Application.Version))
            {
                _error = "Conversion failed: " + (VersionConverter.LastResult?.Error ?? "unknown error");
                return;
            }

            world.LastEditDate = DateTime.Now.ToString();
            WorldInfoSaver.Save(world);
            _error = null;

            var result = VersionConverter.LastResult;
            if (result != null && result.Warnings.Count > 0)
            {
                _result = result;
                return;
            }

            Close();
        }

        private void Close()
        {
            Reset();
            menu.showVersionConvertWindow = false;
        }
    }
}
