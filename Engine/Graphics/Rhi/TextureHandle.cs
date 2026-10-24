namespace Engine.Graphics.Rhi;

public readonly record struct TextureHandle(int Id)
{
    public static readonly TextureHandle None = default;

    public bool IsValid => Id != 0;
}
