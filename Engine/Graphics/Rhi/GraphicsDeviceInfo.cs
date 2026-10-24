namespace Engine.Graphics.Rhi;

public sealed record GraphicsDeviceInfo(
    string Backend,
    string Renderer,
    string Vendor,
    string Version,
    string ShadingLanguageVersion,
    int MaxTextureUnits)
{
    public static readonly GraphicsDeviceInfo None = new("None", "", "", "", "", 0);
}
