namespace Engine.GUI;

public static class ImGuiFontScale
{
    public const float ReferenceWidth = 1280f;
    public const float DefaultStep = 0.25f;
    public const float DefaultMinimum = 1f;

    public static float For(int windowWidth, float step = DefaultStep, float minimum = DefaultMinimum)
    {
        float scale = windowWidth / ReferenceWidth;
        if (step > 0f) scale = MathF.Floor(scale / step + 1e-4f) * step;
        return MathF.Max(minimum, scale);
    }
}
