namespace Engine.Graphics.Rhi;

public readonly record struct VertexArrayHandle(int Id)
{
    public static readonly VertexArrayHandle None = default;

    public bool IsValid => Id != 0;
}
