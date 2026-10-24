using Engine.GUI;

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

    public static IReadOnlyList<InterfaceFont> All { get; } = new[]
    {
        new InterfaceFont(FusionPixelId, "Fusion Pixel (default)", "Resources/Fonts/FusionPixel/FusionPixel12pxMonospacedLatin.ttf", 12),
        new InterfaceFont(SystemId, "Smooth (system font)", null, 14, false)
    };

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

    public static ImGuiFontRequest ToRequest(string id, int size)
    {
        var font = Find(id);
        int pixels = size >= MinSize && size <= MaxSize ? size : font.NativeSize;
        string path = font.RelativePath == null ? null : Path.Combine(AppContext.BaseDirectory, font.RelativePath);
        return new ImGuiFontRequest(path, pixels);
    }
}
