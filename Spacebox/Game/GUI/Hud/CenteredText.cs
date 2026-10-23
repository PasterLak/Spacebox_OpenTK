using Engine;
using Spacebox.Game;
using System.Numerics;


namespace Spacebox.GUI
{
    public static class CenteredText
    {
        private static string _text = "Press RMB to use";

        public static bool IsVisible { get; private set; }

        public static string Text => _text;

        public static OpenTK.Mathematics.Color4 Color => new OpenTK.Mathematics.Color4(_color.X, _color.Y, _color.Z, _color.W);

        private static Vector4 _color = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);

        public static void SetText(string text)
        {
            _text = text;
        }

        public static void SetColor(Vector4 color)
        {
            _color = color;
        }
        public static void SetColor(OpenTK.Mathematics.Color4 color)
        {
            _color = color.ToSystemVector4();
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

        public enum HorizontalAlign
        {
            Left,
            Center,
            Right
        }

        public enum VerticalAlign
        {
            BlockCenter,     
            FirstLineCenter,  
            LastLineCenter   
        }
    }
}
