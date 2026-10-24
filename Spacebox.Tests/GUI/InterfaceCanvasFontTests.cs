using Spacebox.Game;
using Spacebox.Game.GUI.Canvas;

namespace Spacebox.Tests.GUI;

public class InterfaceCanvasFontTests
{
    private static string RepoFile(string relative)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "Spacebox", relative);
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        return null;
    }

    [Fact]
    public void EveryFontFileExists()
    {
        foreach (var font in InterfaceFonts.All)
        {
            if (font.RelativePath == null) continue;
            Assert.True(RepoFile(font.RelativePath) != null, font.RelativePath);
        }
    }

    [Fact]
    public void UnknownIdsFallBackToTheDefault()
    {
        Assert.Equal(InterfaceFonts.DefaultId, InterfaceFonts.Find("nope").Id);
        Assert.Equal(InterfaceFonts.SystemId, InterfaceFonts.Find("SYSTEM").Id);
    }

    [Fact]
    public void Size_OutOfRangeUsesTheNativeSize()
    {
        var font = InterfaceFonts.Find(InterfaceFonts.DefaultId);
        Assert.Equal(font.NativeSize, InterfaceFonts.SizeFor(font, 100));
        Assert.Equal(16, InterfaceFonts.SizeFor(font, 16));
    }

    [Fact]
    public void FontSlot_DetectsChanges()
    {
        Assert.False(InterfaceFontSlot.Differs("fusion_pixel", 12, "FUSION_PIXEL", 12));
        Assert.True(InterfaceFontSlot.Differs("fusion_pixel", 12, "system", 12));
        Assert.True(InterfaceFontSlot.Differs("fusion_pixel", 12, "fusion_pixel", 24));
    }
}
