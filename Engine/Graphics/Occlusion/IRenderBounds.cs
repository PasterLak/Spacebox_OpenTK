using OpenTK.Mathematics;

namespace Engine.Graphics.Occlusion;

public interface IRenderBounds
{
    bool TryGetRenderBounds(out Vector3 min, out Vector3 max);
}
