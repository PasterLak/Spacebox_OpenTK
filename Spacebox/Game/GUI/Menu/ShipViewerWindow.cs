using System.Numerics;
using Engine;
using ImGuiNET;
using Spacebox.Game.ShipViewer;

namespace Spacebox.Game.GUI.Menu;

public sealed class ShipViewerWindow : MenuWindow, IDisposable
{
    private static readonly Vector4 HeaderColor = new Vector4(1f, 0.8f, 0.2f, 1f);
    private static readonly Vector4 WarningColor = new Vector4(1f, 0.75f, 0.3f, 1f);
    private static readonly Vector4 ErrorColor = new Vector4(1f, 0.4f, 0.4f, 1f);

    private readonly GameMenu _menu;
    private readonly ShipViewerSession _session;

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
            if (worlds.Count == 0) ImGui.TextDisabled("No saved worlds");

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

    private static string ShipLabel(SavedShip ship, int index)
    {
        return ship.Name == ship.EntityId.ToString() ? $"Ship {index + 1}" : ship.Name;
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
            else if (!world.CanBeViewed) ImGui.TextWrapped(world.Message);
            else if (_session.IsScanning) ImGui.TextDisabled("Reading ships...");
            else if (result != null)
            {
                if (result.Ships.Count == 0) ImGui.TextDisabled("No player-built ships in this world");
                for (int i = 0; i < result.Ships.Count; i++)
                    DrawShip(result.Ships[i], i);
                if (result.UnreadableFiles > 0) ImGui.TextColored(WarningColor, $"{result.UnreadableFiles} damaged entity files skipped");
            }
        }
        EndList();
    }

    private void DrawShip(SavedShip ship, int index)
    {
        ImGui.PushID(index);
        if (ImGui.Selectable(ShipLabel(ship, index), ReferenceEquals(ship, _session.SelectedShip))) _session.SelectShip(ship);
        if (ImGui.IsItemHovered()) ImGui.SetTooltip($"Entity {ship.EntityId}");
        ImGui.SameLine(0f, 12f);
        ImGui.TextDisabled($"{ship.Stats.Blocks} blocks");
        ImGui.PopID();
    }

    private void DrawPreview()
    {
        var available = ImGui.GetContentRegionAvail();
        float infoHeight = ImGui.GetTextLineHeightWithSpacing() * 3f + ImGui.GetFrameHeightWithSpacing();
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
        ImGui.EndDisabled();

        ImGui.SameLine(0f, 16f);
        ImGui.BeginDisabled(!ready);
        if (ImGui.Button("Save picture")) _session.SavePicture();
        ImGui.EndDisabled();
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) ImGui.SetTooltip("Renders the ship and saves it to the screenshots folder");
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
            ImGui.TextDisabled(total < 0 ? "Reading ship..." : $"Building mesh {content.MeshedChunks}/{total} chunks");
        }
    }
}
