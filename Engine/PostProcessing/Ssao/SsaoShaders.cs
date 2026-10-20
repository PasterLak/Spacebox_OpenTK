namespace Engine.PostProcessing.Ssao;

public sealed record SsaoShaders(Shader Depth, Shader Occlusion, Shader Blur, Shader Composite);
