using ImGuiNET;
using NumericsVector2 = System.Numerics.Vector2;

namespace Engine.Graphics.Atlas;

public readonly struct AtlasSprite
{
    public static readonly AtlasSprite None = default;

    public readonly TextureAtlas Atlas;
    public readonly AtlasRegion Region;

    public AtlasSprite(TextureAtlas atlas, AtlasRegion region)
    {
        Atlas = atlas;
        Region = region;
    }

    public bool IsValid => Atlas != null && Region != null;

    public nint TextureId => IsValid ? Atlas.Texture.Handle : nint.Zero;

    public static AtlasSprite From(TextureAtlas atlas, string name)
    {
        return atlas != null && atlas.TryGetRegion(name, out var region) ? new AtlasSprite(atlas, region) : None;
    }

    public void Draw(ImDrawListPtr drawList, NumericsVector2 min, NumericsVector2 max, uint color = 0xFFFFFFFF)
    {
        if (!IsValid) return;
        drawList.AddImage(TextureId, min, max, Region.ImGuiUv0, Region.ImGuiUv1, color);
    }

    public void Image(NumericsVector2 size)
    {
        if (!IsValid)
        {
            ImGui.Dummy(size);
            return;
        }

        ImGui.Image(TextureId, size, Region.ImGuiUv0, Region.ImGuiUv1);
    }

    public bool ImageButton(string id, NumericsVector2 size)
    {
        if (!IsValid) return ImGui.Button("##" + id, size);
        return ImGui.ImageButton(id, TextureId, size, Region.ImGuiUv0, Region.ImGuiUv1);
    }
}
