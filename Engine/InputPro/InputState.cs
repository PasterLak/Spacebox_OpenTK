using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine.InputPro;

public class InputState
{
    public KeyboardState Keyboard { get; set; }
    public MouseState Mouse { get; set; }
    public Vector2 MousePosition { get; set; }
    public Vector2 MouseDelta { get; set; }
    public Vector2 ScrollDelta { get; set; }
}
