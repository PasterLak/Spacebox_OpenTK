using System.Numerics;
using Engine;
using ImGuiNET;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;
using Spacebox.Game.ShipViewer;

namespace Spacebox.Game.GUI.Menu;

public sealed class ShipViewerWindow : MenuWindow, IDisposable
{
    private static readonly Vector4 HeaderColor = new Vector4(1f, 0.8f, 0.2f, 1f);
    private static readonly Vector4 WarningColor = new Vector4(1f, 0.75f, 0.3f, 1f);
    private static readonly Vector4 ErrorColor = new Vector4(1f, 0.4f, 0.4f, 1f);

    private readonly GameMenu _menu;
    private readonly ShipViewerSession _session;
    private const string ExportPopup = "Export ship##shipViewerExport";
    private string _exportName = "";
    private string _exportAuthor = "";
    private string _exportDescription = "";
    private bool _openExport;

    public ShipViewerWindow(GameMenu menu)
    {
        _menu = menu;
        string worldsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Globals.Menu.WorldsFolder);
        _session = new ShipViewerSession(worldsDirectory, VersionConverter.Registry, Application.Version);
    }

    public override void OnWindowChanged()
    {
        _session.Open();
    }

    public override void OnWindowClosed()
    {
        _session.Close();
    }

    public override void OnSceneRender()
    {
        _session.Update(Time.Delta);
        _session.Render();
    }

    public override void Render()
    {
        var display = ImGui.GetIO().DisplaySize;
        float width = display.X * 0.86f;
        float height = display.Y * 0.84f;
        var position = GameMenu.CenterNextWindow(width, height);

        ImGui.Begin("Ship viewer", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse |
                                   ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
        GameMenu.DrawElementColors(position, new Vector2(width, height), display.Y);

        float padding = height * 0.03f;
        float buttonHeight = height * 0.065f;
        float listWidth = width * 0.3f;
        float contentHeight = height - padding * 3f - buttonHeight;

        ImGui.SetCursorPos(new Vector2(padding, padding));
        if (ImGui.BeginChild("##shipViewerLists", new Vector2(listWidth, contentHeight)))
        {
            DrawWorlds(contentHeight * 0.42f);
            DrawShips();
        }
        ImGui.EndChild();

        ImGui.SetCursorPos(new Vector2(padding * 2f + listWidth, padding));
        if (ImGui.BeginChild("##shipViewerPreview", new Vector2(width - listWidth - padding * 3f, contentHeight), ImGuiChildFlags.None,
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
        {
            DrawPreview();
        }
        ImGui.EndChild();

        float buttonWidth = width * 0.2f;
        _menu.ButtonWithBackground("Back", new Vector2(buttonWidth, buttonHeight), new Vector2((width - buttonWidth) * 0.5f, height - padding - buttonHeight), () => _menu.Open<ModsWindow>());

        ImGui.End();
    }

    public void Dispose()
    {
        _session.Dispose();
    }

    private void DrawWorlds(float listHeight)
    {
        ImGui.TextColored(HeaderColor, "Worlds");
        if (BeginList("##shipViewerWorlds", new Vector2(0f, listHeight)))
        {
            var worlds = _session.Worlds;
            if (_session.IsScanning) ImGui.TextDisabled("Searching worlds for ships...");
            else if (worlds.Count == 0) ImGui.TextDisabled("No worlds with ships");

            for (int i = 0; i < worlds.Count; i++)
                DrawWorld(worlds[i], i);
        }
        EndList();
    }

    private static bool BeginList(string id, Vector2 size)
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(6f, 4f));
        return ImGui.BeginChild(id, size, ImGuiChildFlags.Border | ImGuiChildFlags.AlwaysUseWindowPadding);
    }

    private static void EndList()
    {
        ImGui.EndChild();
        ImGui.PopStyleVar();
    }

    private void DrawWorld(SavedWorld world, int index)
    {
        ImGui.PushID(index);
        if (!world.CanBeViewed) ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled]);

        if (ImGui.Selectable(world.Name, ReferenceEquals(world, _session.SelectedWorld))) _session.SelectWorld(world);
        if (!string.IsNullOrEmpty(world.Message) && ImGui.IsItemHovered()) ImGui.SetTooltip(world.Message);
        ImGui.SameLine(0f, 12f);
        ImGui.TextDisabled($"v{world.GameVersion}  {world.ModId}");

        if (!world.CanBeViewed) ImGui.PopStyleColor();
        ImGui.PopID();
    }

    private void DrawShips()
    {
        ImGui.TextColored(HeaderColor, "Ships");
        if (BeginList("##shipViewerShips", Vector2.Zero))
        {
            var world = _session.SelectedWorld;
            var result = _session.Ships;
            if (world == null) ImGui.TextDisabled("Select a world");
            else if (result != null)
            {
                for (int i = 0; i < result.Ships.Count; i++)
                    DrawShip(result.Ships[i], result.Label(i), i);
                if (result.UnreadableFiles > 0) ImGui.TextColored(WarningColor, $"{result.UnreadableFiles} damaged entity files skipped");
            }
        }
        EndList();
    }

    private void DrawShip(SavedShip ship, string label, int index)
    {
        ImGui.PushID(index);
        if (ImGui.Selectable(label, ReferenceEquals(ship, _session.SelectedShip))) _session.SelectShip(ship);
        if (ImGui.IsItemHovered()) ImGui.SetTooltip($"Entity {ship.EntityId}");
        ImGui.SameLine(0f, 12f);
        ImGui.TextDisabled($"{ship.Stats.Blocks} blocks");
        ImGui.PopID();
    }

    private void DrawPreview()
    {
        var available = ImGui.GetContentRegionAvail();
        float infoHeight = ImGui.GetTextLineHeightWithSpacing() * 3f + ImGui.GetFrameHeightWithSpacing() * 2f;
        _session.DrawViewport(new Vector2(available.X, MathF.Max(32f, available.Y - infoHeight)));
        DrawControls();

        var ship = _session.SelectedShip;
        if (ship == null) ImGui.TextDisabled("Select a ship to view it");
        else DrawShipInfo(ship);

        if (!string.IsNullOrEmpty(_session.Status)) ImGui.TextColored(WarningColor, _session.Status);
        if (!string.IsNullOrEmpty(_session.GameSetNotice)) ImGui.TextColored(WarningColor, _session.GameSetNotice);
        if (_session.SelectedWorld?.Status == SavedWorldStatus.NeedsUpgrade) ImGui.TextColored(WarningColor, _session.SelectedWorld.Message);
    }

    private void DrawControls()
    {
        var content = _session.Content;
        bool ready = content != null && content.IsReady;

        ImGui.Text("Light");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X * 0.2f);
        float ambient = _session.AmbientLight;
        if (ImGui.SliderFloat("##shipViewerLight", ref ambient, 0f, 1.5f, "%.2f")) _session.AmbientLight = ambient;

        ImGui.SameLine(0f, 16f);
        bool powered = _session.Powered;
        if (ImGui.Checkbox("Power", ref powered)) _session.Powered = powered;
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Turns all energy blocks on or off (look only)");

        ImGui.SameLine(0f, 16f);
        ImGui.BeginDisabled(!ready || !content.HasEngines);
        bool engines = _session.EnginesOn;
        if (ImGui.Checkbox("Engines", ref engines)) _session.EnginesOn = engines;
        ImGui.EndDisabled();

        ImGui.SameLine(0f, 16f);
        ImGui.BeginDisabled(!ready || !content.HasCannons);
        if (ImGui.Button("Fire")) _session.FireWeapons();
        ImGui.SameLine(0f, 16f);
        bool autoFire = _session.AutoFire;
        if (ImGui.Checkbox("Autofire", ref autoFire)) _session.AutoFire = autoFire;
        ImGui.EndDisabled();

        ImGui.SameLine(0f, 16f);
        ImGui.BeginDisabled(!ready);
        if (ImGui.Button("Save picture")) _session.SavePicture();
        ImGui.EndDisabled();
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) ImGui.SetTooltip("Renders the ship and saves it to the screenshots folder");

        ImGui.SameLine(0f, 16f);
        ImGui.BeginDisabled(!ready || _session.IsExporting);
        if (ImGui.Button("Export")) BeginExport();
        ImGui.EndDisabled();
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) ImGui.SetTooltip("Saves the ship as one file in the Ships folder to share it; place it in a creative world from the Ships window");
        DrawExportPopup();

        DrawProjectiles();

        ImGui.SameLine(0f, 16f);
        ImGui.BeginDisabled(content == null || !content.CanRestore);
        if (ImGui.Button("Restore ship")) _session.RestoreShip();
        ImGui.EndDisabled();
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) ImGui.SetTooltip("Reloads the ship from its save and removes all damage");
    }

    private void BeginExport()
    {
        var ships = _session.Ships;
        int index = ships?.Ships.IndexOf(_session.SelectedShip) ?? -1;
        _exportName = index >= 0 ? ships.Label(index) : "Ship";
        _exportAuthor = ClientConfigStore.Load()?.PlayerNickname ?? "";
        _exportDescription = "";
        _openExport = true;
    }

    private void DrawExportPopup()
    {
        if (_openExport)
        {
            ImGui.OpenPopup(ExportPopup);
            _openExport = false;
        }

        if (!ImGui.BeginPopupModal(ExportPopup, ImGuiWindowFlags.AlwaysAutoResize)) return;

        ImGui.Text("Name");
        ImGui.SetNextItemWidth(360f);
        ImGui.InputText("##exportName", ref _exportName, 48);
        ImGui.Text("Author");
        ImGui.SetNextItemWidth(360f);
        ImGui.InputText("##exportAuthor", ref _exportAuthor, 32);
        ImGui.Text("Description");
        ImGui.InputTextMultiline("##exportDescription", ref _exportDescription, 400, new Vector2(360f, 80f));
        ImGui.TextDisabled($"File: Ships/{Spacebox.Game.Ships.Sharing.ShipFileNames.MakeId(_exportName)}.ship");

        if (ImGui.Button("Export##exportConfirm", new Vector2(120f, 0f)))
        {
            _session.Export(_exportName, _exportAuthor, _exportDescription);
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (ImGui.Button("Cancel##exportCancel", new Vector2(120f, 0f))) ImGui.CloseCurrentPopup();
        ImGui.EndPopup();
    }

    private void DrawProjectiles()
    {
        ImGui.Text("Shoot");
        ImGui.SameLine();
        string selected = _session.ProjectileName;
        ProjectileParameters current = default;
        bool found = selected != null && GameAssets.TryGetProjectileByName(selected, out current);
        if (found) DrawProjectileColor(current, "##shipViewerShotColor");
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X * 0.3f);
        if (ImGui.BeginCombo("##shipViewerShot", found ? selected : "None", ImGuiComboFlags.HeightLargest))
        {
            if (ImGui.Selectable("None", !found)) _session.ProjectileName = null;
            foreach (var projectile in GameAssets.Projectiles.Values)
            {
                ImGui.PushID(projectile.ID);
                DrawProjectileColor(projectile, "##color");
                if (ImGui.Selectable(projectile.Name, projectile.Name == selected)) _session.ProjectileName = projectile.Name;
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip($"Block damage {projectile.DamageBlocks}, penetration {projectile.Penetration}, up to {projectile.MaximumBlocksCanDestroy} blocks");
                ImGui.PopID();
            }
            ImGui.EndCombo();
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Pick a projectile, then click on the ship to shoot it");
    }

    private static void DrawProjectileColor(ProjectileParameters projectile, string id)
    {
        float size = ImGui.GetFrameHeight();
        var color = new Vector4(projectile.Color.R, projectile.Color.G, projectile.Color.B, 1f);
        ImGui.ColorButton(id, color, ImGuiColorEditFlags.NoTooltip | ImGuiColorEditFlags.NoPicker, new Vector2(size, size));
        ImGui.SameLine();
    }

    private void DrawShipInfo(SavedShip ship)
    {
        var size = ship.Stats.Size;
        ImGui.Text($"Entity {ship.EntityId}  -  {ship.Stats.Blocks} blocks, {ship.Stats.Chunks} chunks, {size.X} x {size.Y} x {size.Z}");

        var content = _session.Content;
        if (content == null) return;

        if (!string.IsNullOrEmpty(content.Error)) ImGui.TextColored(ErrorColor, "Could not load the ship: " + content.Error);
        else if (!content.IsFinished)
        {
            int total = content.TotalChunks;
            if (total < 0) ImGui.TextDisabled("Reading ship...");
            else if (content.MeshedChunks < total) ImGui.TextDisabled($"Building mesh {content.MeshedChunks}/{total} chunks");
        }
    }
}
