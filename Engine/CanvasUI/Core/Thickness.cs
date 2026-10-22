namespace Engine.CanvasUI;

public readonly struct Thickness : IEquatable<Thickness>
{
    public static readonly Thickness Zero = new Thickness(0f);

    public readonly float Left;
    public readonly float Top;
    public readonly float Right;
    public readonly float Bottom;

    public Thickness(float all) : this(all, all, all, all)
    {
    }

    public Thickness(float horizontal, float vertical) : this(horizontal, vertical, horizontal, vertical)
    {
    }

    public Thickness(float left, float top, float right, float bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    public float Horizontal => Left + Right;
    public float Vertical => Top + Bottom;
    public bool IsZero => Left == 0f && Top == 0f && Right == 0f && Bottom == 0f;

    public Rect2 Shrink(Rect2 rect) => rect.Shrink(Left, Top, Right, Bottom);

    public Thickness Scaled(float factor) => new Thickness(Left * factor, Top * factor, Right * factor, Bottom * factor);

    public bool Equals(Thickness other) => Left == other.Left && Top == other.Top && Right == other.Right && Bottom == other.Bottom;
    public override bool Equals(object obj) => obj is Thickness other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Left, Top, Right, Bottom);
    public static bool operator ==(Thickness a, Thickness b) => a.Equals(b);
    public static bool operator !=(Thickness a, Thickness b) => !a.Equals(b);
    public override string ToString() => IsZero ? "0" : $"{Left},{Top},{Right},{Bottom}";
}
