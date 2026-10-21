namespace Engine.CanvasUI;

[Flags]
public enum SizeFlags
{
    ShrinkBegin = 0,
    Fill = 1,
    Expand = 2,
    ExpandFill = Fill | Expand,
    ShrinkCenter = 4,
    ShrinkEnd = 8
}

public enum MouseFilter
{
    Stop,
    Pass,
    Ignore
}

public enum GrowDirection
{
    Begin,
    End,
    Both
}

public enum HorizontalAlignment
{
    Left,
    Center,
    Right
}

public enum VerticalAlignment
{
    Top,
    Center,
    Bottom
}

public enum LayoutPreset
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
    CenterLeft,
    CenterTop,
    CenterRight,
    CenterBottom,
    Center,
    LeftWide,
    TopWide,
    RightWide,
    BottomWide,
    VCenterWide,
    HCenterWide,
    FullRect
}

public enum StretchMode
{
    Disabled,
    KeepAspect,
    Expand
}
