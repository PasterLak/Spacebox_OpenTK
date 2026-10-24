using Engine.CanvasUI;
using OpenTK.Mathematics;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace Engine.GUI
{
    public static class WorldTextDebug
    {

        //private static string _text = "Press F to use";

        public static bool IsVisible { get; private set; } = true;

        private static Vector4 _color = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);

        private static Vector4 red = new Vector4(1.0f, 0, 0, 1.0f);
        private static Vector4 green = new Vector4(0, 1, 0, 1.0f);
        private static Vector4 blue = new Vector4(0, 0, 1, 1.0f);

        public static void SetColor(Vector4 color)
        {
            _color = color;
        }


        public static void Show()
        {
            if (IsVisible) return;

            IsVisible = true;
        }

        public static void Hide()
        {
            if (!IsVisible) return;

            IsVisible = false;
        }

        public static void Toggle()
        {
            IsVisible = !IsVisible;
        }


        public static void Draw(ICanvasRenderer renderer, CanvasFont font, float fontSize, WorldToCanvas projection)
        {

            if (!IsVisible || font == null)
                return;


            //Vector2 textSize = ImGui.CalcTextSize(_text);


            //float posX = (ImGui.GetWindowWidth() - textSize.X) * 0.5f;
            //float posY = (ImGui.GetWindowHeight() - textSize.Y) * 0.5f;

            Label(renderer, font, fontSize, projection, Vector3.Zero, "(0,0,0)", ToColor(_color));
            Label(renderer, font, fontSize, projection, new Vector3(10, 0, 0), "X", ToColor(red));
            Label(renderer, font, fontSize, projection, new Vector3(0, 10, 0), "Y", ToColor(green));
            Label(renderer, font, fontSize, projection, new Vector3(0, 0, 10), "Z", ToColor(blue));
        }

        private static void Label(ICanvasRenderer renderer, CanvasFont font, float fontSize, WorldToCanvas projection, Vector3 world, string text, Color4 color)
        {
            if (!projection.TryProject(world.ToOpenTKVector3(), out var position)) return;
            CanvasDraw.Text(renderer, font, position, text, fontSize, color);
        }

        private static Color4 ToColor(Vector4 color) => new Color4(color.X, color.Y, color.Z, color.W);
    }
}
