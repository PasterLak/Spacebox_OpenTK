namespace Engine.GUI;

public sealed record ImGuiFontRequest(string FilePath, float SizePixels)
{
    public const float BuiltInSize = 13f;

    public static ImGuiFontRequest Default { get; } = new ImGuiFontRequest(null, BuiltInSize);

    public bool IsBuiltIn => string.IsNullOrEmpty(FilePath);
}
