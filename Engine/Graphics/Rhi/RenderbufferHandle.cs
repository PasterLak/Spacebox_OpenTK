namespace Engine.Graphics.Rhi;

public readonly record struct RenderbufferHandle(int Id)
{
    public static readonly RenderbufferHandle None = default;

    public bool IsValid => Id != 0;
}
