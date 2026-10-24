namespace Engine.Graphics.Rhi;

public readonly record struct BufferHandle(int Id)
{
    public static readonly BufferHandle None = default;

    public bool IsValid => Id != 0;
}
