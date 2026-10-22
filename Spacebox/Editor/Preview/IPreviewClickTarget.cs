using OpenTK.Mathematics;

namespace Spacebox.Editor.Preview;

public interface IPreviewClickTarget
{
    bool AcceptsClicks { get; }
    void Click(Vector3 origin, Vector3 direction);
}
