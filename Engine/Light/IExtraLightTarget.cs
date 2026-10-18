using OpenTK.Mathematics;

namespace Engine.Light;

public interface IExtraLightTarget
{
    Vector3 ExtraLight { get; set; }
}
