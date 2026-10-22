using Spacebox.Game.Resource;

namespace Spacebox.Tests.GameSets;

public sealed class ModInstallTargetTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "spacebox_install_" + Guid.NewGuid().ToString("N"));

    public ModInstallTargetTests() => Directory.CreateDirectory(_root);

    public void Dispose() => Directory.Delete(_root, true);

    [Theory]
    [InlineData("..", "pack", "pack")]
    [InlineData("C:\\x", "pack", "C__x")]
    [InlineData("../../evil", "pack", "_.._evil")]
    [InlineData("Default", "pack", "Default_mod")]
    [InlineData("  My Mod. ", "pack", "My Mod")]
    [InlineData("", "", "Mod")]
    public void FolderNames_AreSafe(string name, string fallback, string expected)
    {
        Assert.Equal(expected, ModInstallTarget.SafeFolderName(name, fallback));
    }

    [Fact]
    public void ExistingFolderOfTheSameMod_IsReplaced_OtherModsAreKept()
    {
        Directory.CreateDirectory(Path.Combine(_root, "Lamps"));

        string same = ModInstallTarget.Resolve(_root, "Lamps", "lamps", _ => "lamps", out bool replacesSame);
        string other = ModInstallTarget.Resolve(_root, "Lamps", "tools", _ => "lamps", out bool replacesOther);

        Assert.True(replacesSame);
        Assert.Equal(Path.Combine(_root, "Lamps"), same);
        Assert.False(replacesOther);
        Assert.Equal(Path.Combine(_root, "Lamps_2"), other);
    }

    [Fact]
    public void PathsOutsideTheRoot_AreDetected()
    {
        Assert.True(ModInstallTarget.IsInside(_root, Path.Combine(_root, "a", "b.png")));
        Assert.False(ModInstallTarget.IsInside(_root, Path.Combine(_root, "..", "b.png")));
    }
}
