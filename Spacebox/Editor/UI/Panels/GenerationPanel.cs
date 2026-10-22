using System.Numerics;
using ImGuiNET;
using Spacebox.Editor.Data;
using Spacebox.Editor.Generation;
using Spacebox.Editor.UI.Fields;
using Spacebox.Game.Generation;

namespace Spacebox.Editor.UI.Panels;

public sealed class GenerationPanel : IEditorPanel, IDisposable
{
    private readonly JsonInspector _inspector;
    private readonly IEditorPanel _asteroids;
    private readonly IEditorPanel _biomes;
    private readonly BiomeMapPreview _map = new BiomeMapPreview();
    private string _seed = "spacebox";
    private int[] _sector = new int[3];
    private int _slice = 64;
    private bool _legacyGenerator;
    private bool _showRings = true;

    public string Title => "Generation";

    public GenerationPanel(JsonInspector inspector, IEditorPanel asteroids, IEditorPanel biomes)
    {
        _inspector = inspector;
        _asteroids = asteroids;
        _biomes = biomes;
    }

    public void Draw(EditorContext context)
    {
        if (!ImGui.BeginTabBar("##generationTabs")) return;

        if (ImGui.BeginTabItem("Asteroids"))
        {
            _asteroids.Draw(context);
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("Biome map"))
        {
            DrawBiomeMap(context);
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("Biomes"))
        {
            _biomes.Draw(context);
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("Generator settings"))
        {
            DrawGeneratorSettings(context);
            ImGui.EndTabItem();
        }

        ImGui.EndTabBar();
    }

    public void Dispose()
    {
        _map.Dispose();
    }

    private void DrawBiomeMap(EditorContext context)
    {
        var available = ImGui.GetContentRegionAvail();
        float controlsWidth = MathF.Max(available.X * 0.28f, 200f * EditorWidgets.Scale);

        if (ImGui.BeginChild("##mapControls", new Vector2(controlsWidth, 0f), ImGuiChildFlags.Border))
        {
            ImGui.TextWrapped("Builds the biome map of one sector with the loaded GameSet. Dots are asteroid positions (bright: inside the shown slice).");
            ImGui.Separator();
            ImGui.Text("World seed");
            ImGui.SetNextItemWidth(-1f);
            ImGui.InputText("##seed", ref _seed, 64);
            EditorWidgets.Tooltip("World seed (text or number), the same as in Create World. The same seed always gives the same map.");
            ImGui.Text("Sector index");
            ImGui.SetNextItemWidth(-1f);
            ImGui.InputInt3("##sector", ref _sector[0]);
            EditorWidgets.Tooltip("Which sector to show (x, y, z). One sector is 8192 blocks per side; sector 0,0,0 starts at the world center.");

            if (_map.IsBuilding) ImGui.BeginDisabled();
            ImGui.Checkbox("Old worlds (generator 1)", ref _legacyGenerator);
            EditorWidgets.Tooltip("Show the map as worlds made before 0.1.5 generate it (no distance rules, biome density or shapes).");
            bool build = ImGui.Button("Build", new Vector2(-1f, 0f));
            EditorWidgets.Tooltip("Generates the biome map and asteroid positions of the sector with the saved GameSet files.");
            if (build)
                _map.Build(_seed, new OpenTK.Mathematics.Vector3i(_sector[0], _sector[1], _sector[2]), _legacyGenerator ? Game.Generation.GeneratorVersions.Legacy : Game.Generation.GeneratorVersions.Current);
            if (_map.IsBuilding) ImGui.EndDisabled();

            ImGui.Text("Height slice");
            ImGui.SetNextItemWidth(-1f);
            ImGui.SliderInt("##slice", ref _slice, 0, Game.Generation.Structures.BiomesMap.Resolution - 1);
            EditorWidgets.Tooltip("Height of the horizontal cut through the sector that the map shows (0 = bottom, 127 = top, 64 blocks per step).");
            ImGui.Checkbox("Distance rings", ref _showRings);
            EditorWidgets.Tooltip("Rings show where biomes may start and end: each ring is a biome MinDistanceFromCenter or MaxDistanceFromCenter, measured from the world center (0,0,0) and cut by this height slice. The ring color is the biome color. Biomes without distance limits have no rings.");
            if (_map.HasMap) ImGui.TextDisabled($"{_map.AsteroidCount} asteroids in sector");

            ImGui.Separator();
            DrawLegend();
        }
        ImGui.EndChild();

        ImGui.SameLine();
        if (ImGui.BeginChild("##map", Vector2.Zero, ImGuiChildFlags.Border))
            _map.Draw(ImGui.GetContentRegionAvail(), _slice, _showRings);
        ImGui.EndChild();
    }

    private static void DrawLegend()
    {
        var generator = World.WorldGenerator;
        if (generator?.Biomes == null) return;

        foreach (var biome in generator.Biomes)
        {
            if (biome == null) continue;
            var c = biome.DebugColor;
            ImGui.ColorButton("##" + biome.IdString, new Vector4(c.R / 255f, c.G / 255f, c.B / 255f, 1f), ImGuiColorEditFlags.NoTooltip);
            ImGui.SameLine();
            ImGui.Text($"{biome.Name}  ({biome.AsteroidChances.Count} asteroid types, {DistanceText(biome.Distance)})");
            EditorWidgets.Tooltip($"{biome.Description}\nAppears {DistanceText(biome.Distance)} from the world center, spawn chance {biome.SpawnChance}, density {biome.Density}%.");
        }
    }

    private static string DistanceText(Game.Generation.Structures.DistanceRange range)
    {
        if (range.IsUnlimited) return "everywhere";
        return range.Max == 0 ? $"from {range.Min} blocks" : $"{range.Min}-{range.Max} blocks";
    }

    private void DrawGeneratorSettings(EditorContext context)
    {
        var file = context.Project.GetFile(GameSetCatalog.GeneratorFile);
        ImGui.TextDisabled("Generation changes apply to the previews after Save and Reload.");
        if (ImGui.BeginChild("##generatorProps", Vector2.Zero, ImGuiChildFlags.Border))
        {
            if (_inspector.Draw(context, "##generator", file.Root, GameSetCatalog.FileSchema(GameSetCatalog.GeneratorFile), EditorScopes.Generator))
                file.MarkDirty();
        }
        ImGui.EndChild();
    }
}
