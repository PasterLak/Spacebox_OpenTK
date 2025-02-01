using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Spacebox.Engine
{
    public class InputAction
    {
        public string Name { get; private set; }
        public string Description { get; set; } = "";
        public Keys? Key { get; set; }
        public MouseButton? MouseButton { get; set; }
        public List<Action> Callbacks { get; private set; }
        public bool IsStatic { get; set; }

        public InputAction(string name, Keys key, bool isStatic = false)
        {
            Name = name;
            Key = key;
            IsStatic = isStatic;
            Callbacks = new List<Action>();
        }

        public InputAction(string name, MouseButton button, bool isStatic = false)
        {
            Name = name;
            MouseButton = button;
            IsStatic = isStatic;
            Callbacks = new List<Action>();
        }
    }
}
