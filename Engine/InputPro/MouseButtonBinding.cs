using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine.InputPro;

public class MouseButtonBinding : InputBinding
{
    public MouseButton Button { get; set; }

    public MouseButtonBinding() { }
    public MouseButtonBinding(MouseButton button) => Button = button;

    public override bool IsPressed(InputState state) => state.Mouse.IsButtonPressed(Button);
    public override bool IsReleased(InputState state) => state.Mouse.IsButtonReleased(Button);
    public override bool IsHeld(InputState state) => state.Mouse.IsButtonDown(Button);
    public override string GetDisplayName() => $"Mouse {Button}";
}
