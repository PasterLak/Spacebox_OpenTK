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
    private bool _fit = true;
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
            EditorWidgets.Tooltip("Which texture atlas to show: block or item textures, or their glow (emission) layers.");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X * 0.4f);
            if (ImGui.SliderFloat("Zoom", ref _zoom, 0.05f, 8f, "%.2fx")) _fit = false;
            EditorWidgets.Tooltip("Zoom of the atlas image. Ctrl + mouse wheel over the atlas zooms too.");
            ImGui.SameLine();
            ImGui.Checkbox("Fit", ref _fit);
            EditorWidgets.Tooltip("Zooms the atlas so it always fits the window.");
            ImGui.SameLine();
            if (ImGui.Button("Reload GameSet")) context.RequestReload();
            EditorWidgets.Tooltip("Rebuilds the atlases from the files on disk.");

            if (texture == null || atlas == null)
            {
                ImGui.TextDisabled("Atlas not loaded");
            }
            else
            {
                ImGui.TextDisabled($"{texture.Width}x{texture.Height} px, {atlas.Count} textures");
                if (ImGui.BeginChild("##canvas", Vector2.Zero, ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar))
                {
                    if (_fit) _zoom = FitZoom(texture, ImGui.GetContentRegionAvail());
                    DrawCanvas(atlas, texture);
                }
                ImGui.EndChild();
            }
        }
        ImGui.EndChild();
    }

    public static float FitZoom(Texture2D texture, Vector2 available)
    {
        if (texture.Width <= 0 || texture.Height <= 0 || available.X <= 1f || available.Y <= 1f) return 1f;
        return Math.Clamp(MathF.Min(available.X / texture.Width, available.Y / texture.Height), 0.05f, 8f);
    }

    private void DrawRegions(TextureAtlas atlas)
    {
        ImGui.SetNextItemWidth(-1f);
        ImGui.InputTextWithHint("##filter", "Search...", ref _filter, 128);
        EditorWidgets.Tooltip("Show only textures whose name contains this text.");
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
            if (wheel != 0f && ImGui.GetIO().KeyCtrl)
            {
                _zoom = Math.Clamp(_zoom * MathF.Pow(1.15f, wheel), 0.05f, 8f);
                _fit = false;
            }

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
