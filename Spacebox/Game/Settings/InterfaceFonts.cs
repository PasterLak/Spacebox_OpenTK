namespace Spacebox.Game;

public sealed record InterfaceFont(string Id, string Name, string RelativePath, int NativeSize, bool Pixelated = true);

public static class InterfaceFonts
{
    public const string BuiltInId = "proggy_clean";
    public const string SystemId = "system";
    public const string FusionPixelId = "fusion_pixel";
    public const string DefaultId = FusionPixelId;
    public const int MinSize = 6;
    public const int MaxSize = 32;

    public static IReadOnlyList<InterfaceFont> BuiltIn { get; } = new[]
    {
        new InterfaceFont(FusionPixelId, "Fusion Pixel (default)", "Resources/Fonts/FusionPixel/FusionPixel12pxMonospacedLatin.ttf", 12),
        new InterfaceFont(SystemId, "Smooth (system font)", null, 12, false)
    };

    public static IReadOnlyList<InterfaceFont> All { get; } = Collect();

    private static IReadOnlyList<InterfaceFont> Collect()
    {
        var fonts = new List<InterfaceFont>(BuiltIn);
        try
        {
            fonts.AddRange(InterfaceFontScanner.Scan(AppContext.BaseDirectory, BuiltIn));
        }
        catch (Exception e)
        {
            Engine.Debug.Warning($"[Fonts] Could not scan {InterfaceFontScanner.FontsFolder}: {e.Message}");
        }
        return fonts;
    }

    public static InterfaceFont Find(string id)
    {
        int index = Search(id);
        return All[index < 0 ? 0 : index];
    }

    public static int IndexOf(string id)
    {
        return Math.Max(0, Search(id));
    }

    public static bool IsKnown(string id)
    {
        return Search(id) >= 0;
    }

    private static int Search(string id)
    {
        for (int i = 0; i < All.Count; i++)
        {
            if (string.Equals(All[i].Id, id, StringComparison.OrdinalIgnoreCase)) return i;
        }
        return -1;
    }

    public static int SizeFor(InterfaceFont font, int size) => size >= MinSize && size <= MaxSize ? size : font.NativeSize;

    public static Engine.CanvasUI.SkiaCanvasFont CreateCanvasFont(string id, int size)
    {
        var font = Find(id);
        string path = font.RelativePath == null ? "" : Path.Combine(AppContext.BaseDirectory, font.RelativePath);
        return new Engine.CanvasUI.SkiaCanvasFont(path, SizeFor(font, size), font.Pixelated);
    }
}
