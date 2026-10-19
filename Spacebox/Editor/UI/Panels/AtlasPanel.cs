using System.Numerics;
using Engine;
using Engine.Graphics.Atlas;
using ImGuiNET;
using Spacebox.Game;
using Spacebox.Game.Resource;

namespace Spacebox.Editor.UI.Panels;

public sealed class AtlasPanel : IEditorPanel
{
    private static readonly string[] Views = { "Blocks", "Blocks emission", "Items", "Items emission" };

    private int _view;
    private float _zoom = 1f;
    private string _filter = "";
    private string _selectedRegion;

    public string Title => "Atlases";

    public void Draw(EditorContext context)
    {
        var atlas = _view < 2 ? GameAssets.AtlasBlocks : GameAssets.AtlasItems;
        var texture = _view switch
        {
            0 => GameAssets.BlocksTexture,
            1 => GameAssets.EmissionBlocks,
            2 => GameAssets.ItemsTexture,
            _ => GameAssets.EmissionItems
        };

        var available = ImGui.GetContentRegionAvail();
        float listWidth = MathF.Max(available.X * 0.22f, 150f * EditorWidgets.Scale);

        if (ImGui.BeginChild("##regions", new Vector2(listWidth, 0f), ImGuiChildFlags.Border))
            DrawRegions(atlas);
        ImGui.EndChild();

        ImGui.SameLine();
        if (ImGui.BeginChild("##atlasView", Vector2.Zero, ImGuiChildFlags.Border))
        {
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X * 0.3f);
            ImGui.Combo("##view", ref _view, Views, Views.Length);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X * 0.5f);
            ImGui.SliderFloat("Zoom", ref _zoom, 0.25f, 8f, "%.2fx");
            ImGui.SameLine();
            if (ImGui.Button("Reload GameSet")) context.RequestReload();

            if (texture == null || atlas == null)
            {
                ImGui.TextDisabled("Atlas not loaded");
            }
            else
            {
                ImGui.TextDisabled($"{texture.Width}x{texture.Height} px, {atlas.Count} textures");
                if (ImGui.BeginChild("##canvas", Vector2.Zero, ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar))
                    DrawCanvas(atlas, texture);
                ImGui.EndChild();
            }
        }
        ImGui.EndChild();
    }

    private void DrawRegions(TextureAtlas atlas)
    {
        ImGui.SetNextItemWidth(-1f);
        ImGui.InputTextWithHint("##filter", "Search...", ref _filter, 128);
        if (atlas == null) return;

        float iconSize = ImGui.GetTextLineHeight() * 1.3f;
        if (!ImGui.BeginChild("##regionList")) { ImGui.EndChild(); return; }

        foreach (var name in atlas.Names.OrderBy(n => n))
        {
            if (!string.IsNullOrEmpty(_filter) && name.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) < 0) continue;
            EditorWidgets.Icon(atlas.GetSprite(name), iconSize);
            ImGui.SameLine();
            if (ImGui.Selectable(name, name == _selectedRegion, ImGuiSelectableFlags.None, new Vector2(0f, iconSize))) _selectedRegion = name;
        }
        ImGui.EndChild();
    }

    private void DrawCanvas(TextureAtlas atlas, Texture2D texture)
    {
        var size = new Vector2(texture.Width, texture.Height) * _zoom;
        var origin = ImGui.GetCursorScreenPos();
        var drawList = ImGui.GetWindowDrawList();
        drawList.AddRectFilled(origin, origin + size, 0xFF202020);
        ImGui.Image(texture.Handle, size, new Vector2(0, 1), new Vector2(1, 0));

        if (ImGui.IsItemHovered())
        {
            var mouse = (ImGui.GetMousePos() - origin) / _zoom;
            float wheel = ImGui.GetIO().MouseWheel;
            if (wheel != 0f && ImGui.GetIO().KeyCtrl) _zoom = Math.Clamp(_zoom * MathF.Pow(1.15f, wheel), 0.25f, 8f);

            var hovered = FindRegion(atlas, texture, mouse);
            if (hovered != null)
            {
                Outline(drawList, origin, hovered, texture, 0xFF00FFFF);
                ImGui.SetTooltip($"{hovered.Name}  {hovered.Width}x{hovered.Height}");
                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left)) _selectedRegion = hovered.Name;
            }
        }

        if (_selectedRegion != null && atlas.TryGetRegion(_selectedRegion, out var selected))
            Outline(drawList, origin, selected, texture, 0xFF00A5FF);
    }

    private void Outline(ImDrawListPtr drawList, Vector2 origin, AtlasRegion region, Texture2D texture, uint color)
    {
        var min = origin + new Vector2(region.UvMin.X * texture.Width, (1f - region.UvMax.Y) * texture.Height) * _zoom;
        var max = origin + new Vector2(region.UvMax.X * texture.Width, (1f - region.UvMin.Y) * texture.Height) * _zoom;
        drawList.AddRect(min, max, color, 0f, ImDrawFlags.None, 2f);
    }

    private static AtlasRegion FindRegion(TextureAtlas atlas, Texture2D texture, Vector2 pixel)
    {
        var uv = new Vector2(pixel.X / texture.Width, 1f - pixel.Y / texture.Height);
        foreach (var name in atlas.Names)
        {
            if (!atlas.TryGetRegion(name, out var region)) continue;
            if (uv.X >= region.UvMin.X && uv.X <= region.UvMax.X && uv.Y >= region.UvMin.Y && uv.Y <= region.UvMax.Y) return region;
        }
        return null;
    }
}
