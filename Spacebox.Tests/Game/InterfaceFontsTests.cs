using System.Text.Json;
using Spacebox.Game;

namespace Spacebox.Tests.Game;

public sealed class InterfaceFontsTests
{
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
    public void FontFile_Exists(string id)
    {
        var font = InterfaceFonts.Find(id);
        string path = Path.Combine(GameFolder(), font.RelativePath);
        Assert.True(File.Exists(path), path);
    }

    [Fact]
    public void SizeFor_UsesNativeSizeWhenSizeIsOutOfRange()
    {
        var font = InterfaceFonts.Find(InterfaceFonts.FusionPixelId);
        Assert.Equal(12, InterfaceFonts.SizeFor(font, 0));
        Assert.Equal(20, InterfaceFonts.SizeFor(font, 20));
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
