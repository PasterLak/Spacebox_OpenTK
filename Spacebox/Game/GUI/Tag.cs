using Engine;
using ImGuiNET;
using OpenTK.Mathematics;

namespace Spacebox.Game.GUI
{
    public class Tag
    {
        public Guid Id { get; private set; }
        public string Text { get; set; }
        public Vector3 WorldPosition { get; set; }
        public Color4 Color { get; set; }
        public bool IsStatic { get; set; }
        public Alignment TextAlignment { get; set; }
        public uint ColorUint { get; private set; }
        public bool Enabled { get; set; }

        private static float MinFontSize;
        private static float MaxFontSize;

        public Tag()
        {
            Id = Guid.NewGuid();
            Reset();
        }

        public void Initialize(string text, Vector3 worldPosition, Color4 color, bool isStatic = false, Alignment alignment = Alignment.Center)
        {
            Text = text;
            WorldPosition = worldPosition;
            Color = color;
            IsStatic = isStatic;
            TextAlignment = alignment;
            Enabled = true;
            UpdateColorUint();
        }

        public void Reset()
        {
            Text = string.Empty;
            WorldPosition = Vector3.Zero;
            Color = Color4.White;
            IsStatic = false;
            TextAlignment = Alignment.Center;
            ColorUint = 0;
            Enabled = false;
        }

        private void UpdateColorUint()
        {
            ColorUint = ImGui.GetColorU32(new System.Numerics.Vector4(Color.R, Color.G, Color.B, Color.A));
        }

        public void UpdateColor(Color4 newColor)
        {
            Color = newColor;
            UpdateColorUint();
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
            return TextAlignment switch
            {
                Alignment.Left => screenPosition - textSize,
                Alignment.Right => screenPosition,
                Alignment.Center => screenPosition - textSize * 0.5f,
                _ => screenPosition - textSize * 0.5f
            };
        }

        public enum Alignment : byte
        {
            Left,
            Center,
            Right
        }
    }
}