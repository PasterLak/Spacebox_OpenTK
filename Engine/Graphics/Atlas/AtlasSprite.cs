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
}
