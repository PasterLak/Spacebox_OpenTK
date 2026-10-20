using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine.InputPro;

public class KeyBinding : InputBinding
{
    public Keys Key { get; set; }

    public KeyBinding() { }
    public KeyBinding(Keys key) => Key = key;

    public override bool IsPressed(InputState state) => state.Keyboard.IsKeyPressed(Key);
    public override bool IsReleased(InputState state) => state.Keyboard.IsKeyReleased(Key);
    public override bool IsHeld(InputState state) => state.Keyboard.IsKeyDown(Key);
    public override string GetDisplayName() => Key.ToString();
}
