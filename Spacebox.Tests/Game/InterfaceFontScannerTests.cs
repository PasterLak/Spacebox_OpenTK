using Spacebox.Game;

namespace Spacebox.Tests.Game;

public sealed class InterfaceFontScannerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "spacebox-fonts-" + Guid.NewGuid().ToString("N"));

    public InterfaceFontScannerTests()
    {
        Directory.CreateDirectory(Path.Combine(_root, "Resources", "Fonts", "Extra"));
    }

    public void Dispose()
    {
        Directory.Delete(_root, true);
    }

    private void AddFile(string relative)
    {
        string path = Path.Combine(_root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, new byte[] { 0 });
    }

    [Fact]
    public void Scan_FindsNewFontFilesAndSkipsKnownOnes()
    {
        AddFile("Resources/Fonts/Known.ttf");
        AddFile("Resources/Fonts/Extra/CoolPixel.otf");
        AddFile("Resources/Fonts/Smooth Sans.TTF");
        AddFile("Resources/Fonts/readme.txt");
        var known = new[] { new InterfaceFont("known", "Known", "Resources/Fonts/Known.ttf", 12) };

        var found = InterfaceFontScanner.Scan(_root, known);

        Assert.Equal(2, found.Count);
        var pixel = found.Single(f => f.Name == "CoolPixel");
        Assert.Equal("file:extra/coolpixel", pixel.Id);
        Assert.Equal("Resources/Fonts/Extra/CoolPixel.otf", pixel.RelativePath);
        Assert.True(pixel.Pixelated);
        var smooth = found.Single(f => f.Name == "Smooth Sans");
        Assert.False(smooth.Pixelated);
        Assert.Equal(InterfaceFontScanner.DefaultSize, smooth.NativeSize);
    }

    [Fact]
    public void Scan_MissingFolder_ReturnsNothing()
    {
        Assert.Empty(InterfaceFontScanner.Scan(Path.Combine(_root, "nothing"), Array.Empty<InterfaceFont>()));
    }

    [Fact]
    public void SystemFont_DefaultsTo12()
    {
        Assert.Equal(12, InterfaceFonts.Find(InterfaceFonts.SystemId).NativeSize);
    }
}
