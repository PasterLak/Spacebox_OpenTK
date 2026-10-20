using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine.InputPro;

public class MouseKeyBinding : InputBinding
{
    public MouseButton Key { get; set; }

    public MouseKeyBinding() { }
    public MouseKeyBinding(MouseButton button) => Key = button;

    public override bool IsPressed(InputState state) => state.Mouse.IsButtonPressed(Key);
    public override bool IsReleased(InputState state) => state.Mouse.IsButtonReleased(Key);
    public override bool IsHeld(InputState state) => state.Mouse.IsButtonDown(Key);
    public override string GetDisplayName() => $"Mouse {Key}";
}
