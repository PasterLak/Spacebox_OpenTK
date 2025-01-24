using ImGuiNET;
using OpenTK.Mathematics;

namespace Spacebox.Game.GUI
{
    public class Tag
    {
        public Guid Id { get; set; }
        public string Text { get; set; }
        public float FontSize { get; set; } = 40f;
        public Vector3 WorldPosition { get; set; }
        public Color4 Color { get; set; }
        public bool IsStatic { get; set; }
        public Alignment TextAlignment { get; set; } = Alignment.Center;

        private static float MinFontSize;
        private static float MaxFontSize;

        public uint ColorUint;
        public Tag(string text, Vector3 worldPosition, Color4 color, bool isStatic = false)
        {
            Id = new Guid();
            Text = text;
            WorldPosition = worldPosition;
            Color = color;
            IsStatic = isStatic;

            ColorUint = ImGui.GetColorU32(new System.Numerics.Vector4(Color.R, Color.G, Color.B, Color.A));

            SetFontSizes();

             FontSize = MaxFontSize;
        }

        public static void SetFontSizes()
        {
            MinFontSize = Window.Instance.Size.Y / 100 * 1f;
            MaxFontSize = Window.Instance.Size.Y / 100 * 2f;
        }

        public void SetFontSizeFromDistance(float distance)
        {
            FontSize = CalculateFontSize(distance);
        }
        private float CalculateFontSize(float distance)
        {
            if (distance <= 0f) return MaxFontSize;
            if (distance >= 8000f) return MinFontSize;
            return MathHelper.Clamp(MaxFontSize - (MaxFontSize - MinFontSize) * (distance / 8000f), MinFontSize, MaxFontSize);
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