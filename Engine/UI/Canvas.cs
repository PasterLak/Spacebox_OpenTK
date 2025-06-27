
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;


namespace Engine.UI
{
    public class Canvas : Node2D
    {
        public Vector2i VirtualSize { get; set; } = new Vector2i(1920, 1080);

        public Vector2i ScreenSize { get; private set; }

        public Canvas(Vector2i virtualSize, GameWindow window)
        {
            VirtualSize = virtualSize;

            ScreenSize = window.ClientSize;
            //Debug.Log($"sizes real {ScreenSize} virt {virtualSize}");
            window.Resize += Resize;
        }

        public void Resize(ResizeEventArgs arg)
        {
            ScreenSize = arg.Size;
           // Debug.Log($"sizes real {ScreenSize} virt {VirtualSize}");
            foreach (var child in Children)
            {

                child.OnResized(this);
            }
        }

        public override void Draw()
        {
            foreach (var child in Children)
            {

                child.Draw();
            }
        }
    }
}
