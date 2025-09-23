using ImGuiNET;
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
        }

        public static void SetFontSizes(Vector2 displaySize)
        {
            MinFontSize = displaySize.Y * 0.01f;
            MaxFontSize = displaySize.Y * 0.016f;
        }

        public static float CalculateFontSize(float distanceSquared)
        {
            const int maxDis = 1000 * 1000;
            if (distanceSquared <= 0f)
                return MaxFontSize;
            if (distanceSquared >= maxDis)
                return MinFontSize;
            float size = MaxFontSize - (MaxFontSize - MinFontSize) * (distanceSquared / maxDis);
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
