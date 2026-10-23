using OpenTK.Mathematics;

namespace Spacebox.Game.GUI;

public class Tag
{
    public Guid Id { get; private set; }
    public string Text { get; set; }
    public Vector3 WorldPosition { get; set; }
    public Color4 Color { get; set; }
    public bool IsStatic { get; set; }
    public Alignment TextAlignment { get; set; }
    public uint ColorUint { get; private set; }
    public bool Visible { get; set; }
    public TagCategory Category { get; set; }

    public const float MinFontShare = 0.7f / 60f;
    public const float MaxFontShare = 0.7f / 40f;

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
        Category = isStatic ? TagCategory.Waypoint : TagCategory.General;
        Visible = true;
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
        Visible = false;
        Category = TagCategory.General;
    }

    private void UpdateColorUint()
    {
        ColorUint = (uint)(Pack(Color.A) << 24 | Pack(Color.B) << 16 | Pack(Color.G) << 8 | Pack(Color.R));
    }

    private static int Pack(float channel) => (int)MathF.Round(Math.Clamp(channel, 0f, 1f) * 255f);

    public void UpdateColor(Color4 newColor)
    {
        Color = newColor;
        UpdateColorUint();
    }

    public static void SetFontSizes(Vector2 displaySize)
    {
        MinFontSize = displaySize.Y * MinFontShare;
        MaxFontSize = displaySize.Y * MaxFontShare;
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

    public Vector2 GetTextPosition(Vector2 screenPosition, Vector2 textSize)
    {
        return TextAlignment switch
        {
            Alignment.Left => screenPosition - textSize,
            Alignment.Right => screenPosition,
            Alignment.Center => screenPosition - textSize * 0.5f,
            _ => screenPosition - textSize * 0.5f
        };
    }

    public enum TagCategory : byte
    {
        General,
        Entity,
        Waypoint
    }

    public enum Alignment : byte
    {
        Left,
        Center,
        Right
    }
}