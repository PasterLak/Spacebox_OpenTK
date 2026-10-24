namespace Engine.Graphics.Rhi;

public readonly record struct ShaderSources(string Vertex, string Fragment, string? Geometry = null)
{
    public bool HasGeometry => !string.IsNullOrWhiteSpace(Geometry);
}
