using System.Globalization;
using OpenTK.Mathematics;

namespace Engine.CanvasUI.Validation;

public readonly record struct ScreenProfile(string Name, int Width, int Height, float UiScale = 1f)
{
    public Thickness SafeMargins { get; init; }

    public Vector2 Size => new Vector2(Width, Height);
    public float Aspect => (float)Width / Height;

    public ScreenProfile WithUiScale(float scale) => this with { UiScale = scale };

    public ScreenProfile WithSafeMargins(Thickness margins) => this with { SafeMargins = margins };

    public override string ToString()
    {
        string text = $"{Name} {Width}x{Height}";
        if (UiScale != 1f) text += " @" + UiScale.ToString("0.##", CultureInfo.InvariantCulture) + "x";
        if (!SafeMargins.IsZero) text += " safe " + SafeMargins;
        return text;
    }
}

public static class ScreenProfiles
{
    public static readonly ScreenProfile[] Widescreen =
    {
        new("HD", 1280, 720),
        new("WXGA", 1366, 768),
        new("HD+", 1600, 900),
        new("FullHD", 1920, 1080),
        new("QHD", 2560, 1440),
        new("4K", 3840, 2160),
        new("5K", 5120, 2880)
    };

    public static readonly ScreenProfile[] Wide16x10 =
    {
        new("WXGA 16:10", 1280, 800),
        new("WXGA+", 1440, 900),
        new("WSXGA+", 1680, 1050),
        new("WUXGA", 1920, 1200),
        new("WQXGA", 2560, 1600)
    };

    public static readonly ScreenProfile[] Classic =
    {
        new("SVGA 4:3", 800, 600),
        new("XGA 4:3", 1024, 768),
        new("UXGA 4:3", 1600, 1200),
        new("SXGA 5:4", 1280, 1024)
    };

    public static readonly ScreenProfile[] Ultrawide =
    {
        new("UW 21:9", 2560, 1080),
        new("UWQHD 21:9", 3440, 1440),
        new("UW5K 21:9", 5120, 2160),
        new("DFHD 32:9", 3840, 1080),
        new("DQHD 32:9", 5120, 1440)
    };

    public static readonly ScreenProfile[] Portrait =
    {
        new("Portrait 9:16", 1080, 1920),
        new("Portrait 3:4", 1200, 1600),
        new("Portrait 4:5", 1080, 1350)
    };

    public static readonly ScreenProfile[] UiScaled =
    {
        new("HD", 1280, 720, 1.25f),
        new("FullHD", 1920, 1080, 1.5f),
        new("QHD", 2560, 1440, 2f),
        new("4K", 3840, 2160, 2f),
        new("SVGA 4:3", 800, 600, 1.5f),
        new("FullHD", 1920, 1080, 0.75f)
    };

    public static readonly ScreenProfile[] SafeAreas =
    {
        new ScreenProfile("TV overscan", 1920, 1080).WithSafeMargins(new Thickness(96f, 54f)),
        new ScreenProfile("Notch 20:9", 2400, 1080).WithSafeMargins(new Thickness(110f, 0f, 110f, 0f)),
        new ScreenProfile("Rounded corners", 2560, 1600).WithSafeMargins(new Thickness(24f, 24f, 24f, 40f))
    };

    public static IReadOnlyList<ScreenProfile> All { get; } = Concat(Widescreen, Wide16x10, Classic, Ultrawide, Portrait, UiScaled, SafeAreas);

    public static IReadOnlyList<ScreenProfile> Quick { get; } = new[] { Widescreen[0], Widescreen[3], Classic[0], Classic[3], Ultrawide[4], Portrait[0], UiScaled[1] };

    private static ScreenProfile[] Concat(params ScreenProfile[][] groups)
    {
        var all = new List<ScreenProfile>();
        foreach (var group in groups) all.AddRange(group);
        return all.ToArray();
    }
}
