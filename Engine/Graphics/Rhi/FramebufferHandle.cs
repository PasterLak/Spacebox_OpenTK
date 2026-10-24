namespace Engine.Graphics.Rhi;

public readonly record struct FramebufferHandle(int Id)
{
    public static readonly FramebufferHandle Default = default;

    public bool IsDefault => Id == 0;
}
