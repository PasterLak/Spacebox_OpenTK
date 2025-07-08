using ImGuiNET;
using OpenTK.Mathematics;
using Engine;

namespace Spacebox.Game.GUI
{
    public class Tag
    {
        public Guid Id { get; set; }
        public string Text { get; set; }
        public float FontSize { get; set; }
        public Vector3 WorldPosition { get; set; }
        public Color4 Color { get; set; }
        public bool IsStatic { get; set; }
        public Alignment TextAlignment { get; set; } = Alignment.Center;
        public uint ColorUint;

        private static float MinFontSize;
        private static float MaxFontSize;

        public bool Enabled { get; set; }

        public Tag(string text, Vector3 worldPosition, Color4 color, bool isStatic = false)
        {
            Id = Guid.NewGuid();
            Text = text;
            WorldPosition = worldPosition;
            Color = color;
            IsStatic = isStatic;
            ColorUint = ImGui.GetColorU32(new System.Numerics.Vector4(Color.R, Color.G, Color.B, Color.A));

            FontSize = CalculateFontSize(100);
        }

        public static void SetFontSizes(Vector2 displaySize)
        {
            MinFontSize = displaySize.Y * 0.01f;
            MaxFontSize = displaySize.Y * 0.02f;
        }

        public static float CalculateFontSize(float distance)
        {

            if (distance <= 0f)
                return MaxFontSize;
            if (distance >= 18000f)
                return MinFontSize;
            float size = MaxFontSize - (MaxFontSize - MinFontSize) * (distance / 18000f);
            return MathHelper.Clamp(size, MinFontSize, MaxFontSize);
        }

        public System.Numerics.Vector2 GetTextPosition(System.Numerics.Vector2 screenPosition, System.Numerics.Vector2 textSize)
        {
            if (TextAlignment == Alignment.Left)
                return screenPosition - textSize;
            if (TextAlignment == Alignment.Right)
                return screenPosition;
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
