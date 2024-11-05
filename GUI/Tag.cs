using OpenTK.Mathematics;

namespace Spacebox.GUI
{
    public class Tag
    {
        public string Text { get; set; }
        public Vector3 WorldPosition { get; set; }
        public Color4 Color { get; set; }
        public bool IsStatic { get; set; } // Если true, позиция не будет обновляться

        public Tag(string text, Vector3 worldPosition, Color4 color, bool isStatic = false)
        {
            Text = text;
            WorldPosition = worldPosition;
            Color = color;
            IsStatic = isStatic;
        }
    }
}
