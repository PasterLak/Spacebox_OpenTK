using System.Text.Json;
using Engine.GUI;
using ImGuiNET;
using Spacebox.Game;

namespace Spacebox.Tests.Game;

public sealed class InterfaceFontsTests : IDisposable
{
    private readonly IntPtr _previous;
    private readonly IntPtr _context;

    public InterfaceFontsTests()
    {
        _previous = ImGui.GetCurrentContext();
        _context = ImGui.CreateContext();
        ImGui.SetCurrentContext(_context);
    }

    public void Dispose()
    {
        ImGui.DestroyContext(_context);
        ImGui.SetCurrentContext(_previous);
    }

    private static string GameFolder()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Spacebox", "Resources", "Fonts"))) dir = dir.Parent;
        Assert.NotNull(dir);
        return Path.Combine(dir!.FullName, "Spacebox");
    }

    public static IEnumerable<object[]> FileFonts()
    {
        return InterfaceFonts.All.Where(font => font.RelativePath != null).Select(font => new object[] { font.Id });
    }

    [Theory]
    [MemberData(nameof(FileFonts))]
    public void FontFile_LoadsWithLatinAndCyrillicLetters(string id)
    {
        var font = InterfaceFonts.Find(id);
        string path = Path.Combine(GameFolder(), font.RelativePath);
        Assert.True(File.Exists(path), path);

        var fonts = ImGui.GetIO().Fonts;
        ImGuiFontLoader.Load(fonts, new ImGuiFontRequest(path, font.NativeSize));
        fonts.GetTexDataAsRGBA32(out IntPtr _, out int width, out int height);

        Assert.True(width > 0 && height > 0);
        var loaded = fonts.Fonts[0];
        foreach (char letter in "AzÄößЖяéçñ¡¿")
            Assert.True(ImGuiFontLoader.HasGlyph(loaded, letter), $"{id} has no '{letter}'");
    }

    [Fact]
    public void BuiltInFont_LoadsWithoutFile()
    {
        var fonts = ImGui.GetIO().Fonts;
        ImGuiFontLoader.Load(fonts, ImGuiFontRequest.Default);
        fonts.GetTexDataAsRGBA32(out IntPtr _, out int width, out int _);

        Assert.True(width > 0);
        Assert.True(ImGuiFontLoader.HasGlyph(fonts.Fonts[0], 'A'));
    }

    [Fact]
    public void MissingFontFile_FallsBackToBuiltIn()
    {
        var fonts = ImGui.GetIO().Fonts;
        ImGuiFontLoader.Load(fonts, new ImGuiFontRequest(Path.Combine(GameFolder(), "missing.ttf"), 14));
        fonts.GetTexDataAsRGBA32(out IntPtr _, out int width, out int _);

        Assert.True(width > 0);
        Assert.Equal(1, fonts.Fonts.Size);
    }

    [Fact]
    public void ToRequest_UsesNativeSizeWhenSizeIsOutOfRange()
    {
        Assert.Equal(12f, InterfaceFonts.ToRequest(InterfaceFonts.FusionPixelId, 0).SizePixels);
        Assert.Equal(20f, InterfaceFonts.ToRequest(InterfaceFonts.FusionPixelId, 20).SizePixels);
        Assert.True(InterfaceFonts.ToRequest(InterfaceFonts.SystemId, 14).IsBuiltIn);
        Assert.False(InterfaceFonts.ToRequest("unknown", 13).IsBuiltIn);
    }

    [Fact]
    public void Settings_ValidateFontAndSize()
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var settings = JsonSerializer.Deserialize<GameSettings>("{ \"interface\": { \"font\": \"comic_sans\", \"font_size\": 99 } }", options)!;
        settings.ValidateAgainst(GameSettings.Default());

        Assert.Equal(InterfaceFonts.FusionPixelId, settings.Interface.Font);
        Assert.Equal(12, settings.Interface.FontSize);

        var builtIn = JsonSerializer.Deserialize<GameSettings>("{ \"interface\": { \"font\": \"proggy_clean\", \"font_size\": 2 } }", options)!;
        builtIn.ValidateAgainst(GameSettings.Default());
        Assert.Equal(InterfaceFonts.FusionPixelId, builtIn.Interface.Font);
        Assert.Equal(12, builtIn.Interface.FontSize);

        var old = JsonSerializer.Deserialize<GameSettings>("{ \"graphics\": { \"fov\": 70 } }", options)!;
        old.ValidateAgainst(GameSettings.Default());
        Assert.Equal(InterfaceFonts.FusionPixelId, old.Interface.Font);
        Assert.Equal(12, old.Interface.FontSize);
    }
}
