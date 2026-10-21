namespace Engine.Graphics.Occlusion;

public interface IOcclusionCullable : IRenderBounds
{
    OcclusionState Occlusion { get; }
}
