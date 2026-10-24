namespace Engine.Graphics.Rhi;

public readonly record struct ProgramHandle(int Id)
{
    public static readonly ProgramHandle None = default;

    public bool IsValid => Id != 0;
}
