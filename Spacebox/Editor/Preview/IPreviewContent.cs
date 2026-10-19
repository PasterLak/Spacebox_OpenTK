using OpenTK.Mathematics;

namespace Spacebox.Editor.Preview;

public interface IPreviewContent : IDisposable
{
    Vector3 Center { get; }
    float Radius { get; }
    bool IsReady { get; }
    Vector2 ViewAngles => new Vector2(35f, 25f);
    void Update(float deltaTime);
    void Render();
}
