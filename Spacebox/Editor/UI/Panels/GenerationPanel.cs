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
            ImGui.Text("Sector index");
            ImGui.SetNextItemWidth(-1f);
            ImGui.InputInt3("##sector", ref _sector[0]);

            if (_map.IsBuilding) ImGui.BeginDisabled();
            if (ImGui.Button("Build", new Vector2(-1f, 0f))) _map.Build(_seed, new OpenTK.Mathematics.Vector3i(_sector[0], _sector[1], _sector[2]));
            if (_map.IsBuilding) ImGui.EndDisabled();

            ImGui.Text("Height slice");
            ImGui.SetNextItemWidth(-1f);
            ImGui.SliderInt("##slice", ref _slice, 0, Game.Generation.Structures.BiomesMap.Resolution - 1);
            if (_map.HasMap) ImGui.TextDisabled($"{_map.AsteroidCount} asteroids in sector");

            ImGui.Separator();
            DrawLegend();
        }
        ImGui.EndChild();

        ImGui.SameLine();
        if (ImGui.BeginChild("##map", Vector2.Zero, ImGuiChildFlags.Border))
            _map.Draw(ImGui.GetContentRegionAvail(), _slice);
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
            ImGui.Text($"{biome.Name}  ({biome.AsteroidChances.Count} asteroid types)");
        }
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
