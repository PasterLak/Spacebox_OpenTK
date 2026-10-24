namespace Engine.Graphics.Rhi;

public readonly record struct QueryHandle(int Id)
{
    public static readonly QueryHandle None = default;

    public bool IsValid => Id != 0;
}
