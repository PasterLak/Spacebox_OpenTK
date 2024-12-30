using OpenTK.Mathematics;

namespace Spacebox.Game.GUI
{
    public class Tag
    {
        public Guid Id { get; set; }
        public string Text { get; set; }
        public Vector3 WorldPosition { get; set; }
        public Color4 Color { get; set; }
        public bool IsStatic { get; set; }
        public Alignment TextAlignment { get; set; } = Alignment.Center;

        public Tag(string text, Vector3 worldPosition, Color4 color, bool isStatic = false)
        {
            Id = new Guid();
            Text = text;
            WorldPosition = worldPosition;
            Color = color;
            IsStatic = isStatic;
        }

        public System.Numerics.Vector2 GetTextPosition(System.Numerics.Vector2 screenPosition,
            System.Numerics.Vector2 textSize)
        {
            if (TextAlignment == Alignment.Left)
            {
                return screenPosition - textSize;
            }

            if (TextAlignment == Alignment.Right)
            {
                return screenPosition;
            }

            return screenPosition - textSize * 0.5f;
        }

        public enum Alignment : byte
        {
            Left,
            Center,
            Right
        }
    }
}