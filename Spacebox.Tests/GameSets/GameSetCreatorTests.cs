using System.Text.Json;
using Spacebox.Game.Resource.Content;

namespace Spacebox.Tests.GameSets;

public sealed class GameSetCreatorTests : IDisposable
{
    private readonly TempGameSets _sets = new TempGameSets();

    public void Dispose() => _sets.Dispose();

    private ModFolders Folders => new ModFolders(_sets.Root);

    private List<InstalledGameSet> Installed() => InstalledGameSets.Scan(Folders);

    private void WriteBaseSets()
    {
        _sets.Config("GameSets/Empty", "empty");
        _sets.Write("GameSets/Empty/icon.png", "empty-icon");
        _sets.Config("GameSets/Default", "default", "empty");
        _sets.Write("GameSets/Default/icon.png", "default-icon");
    }

    private static JsonElement Config(string folder)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, GameSetFormat.ConfigFile)));
        return document.RootElement.Clone();
    }

    [Theory]
    [InlineData("My Space Set", "my_space_set")]
    [InlineData("  Über--Mod 2 ", "ber_mod_2")]
    [InlineData("!!!", "")]
    public void MakeId_KeepsLettersAndDigits(string name, string expected)
    {
        Assert.Equal(expected, GameSetCreator.MakeId(name));
    }

    [Fact]
    public void NewGameSet_IsBuiltOnEmptyAndListedAsGameSet()
    {
        WriteBaseSets();

        var result = GameSetCreator.Create(Folders, new NewGameSetRequest { Kind = NewGameSetKind.GameSet, Name = "My Set", Author = "Me" }, Installed());

        Assert.True(result.Success, result.Error);
        Assert.Equal(Path.Combine(Folders.GameSets, "My Set"), result.Folder);
        Assert.Equal("my_set", Config(result.Folder).GetProperty("ModId").GetString());
        Assert.Equal("empty", Config(result.Folder).GetProperty("Extends").GetString());
        Assert.Equal("empty-icon", File.ReadAllText(Path.Combine(result.Folder, "icon.png")));
        var created = InstalledGameSets.Find(Installed(), "my_set");
        Assert.False(created.IsExtension);
        Assert.True(created.IsBasedOnEmpty);
    }

    [Fact]
    public void NewExtension_GoesToExtensionsAndExtendsTheChosenGameSet()
    {
        WriteBaseSets();

        var result = GameSetCreator.Create(Folders, new NewGameSetRequest { Kind = NewGameSetKind.Extension, Name = "Lamps", BaseId = "default" }, Installed());

        Assert.True(result.Success, result.Error);
        Assert.Equal(Path.Combine(Folders.Extensions, "Lamps"), result.Folder);
        Assert.Equal("default", Config(result.Folder).GetProperty("Extends").GetString());
        Assert.Equal("default-icon", File.ReadAllText(Path.Combine(result.Folder, "icon.png")));
        Assert.True(InstalledGameSets.Find(Installed(), "lamps").IsExtension);
    }

    [Fact]
    public void Empty_CannotBeExtended()
    {
        WriteBaseSets();

        Assert.DoesNotContain(GameSetCreator.ExtendableBases(Installed()), g => g.IsEmpty);
        Assert.NotNull(GameSetCreator.Validate(new NewGameSetRequest { Kind = NewGameSetKind.Extension, Name = "X", BaseId = "empty" }, Installed()));
    }

    [Fact]
    public void TakenIdsAndFolders_GetANumber()
    {
        WriteBaseSets();
        GameSetCreator.Create(Folders, new NewGameSetRequest { Name = "Pack" }, Installed());

        var second = GameSetCreator.Create(Folders, new NewGameSetRequest { Name = "Pack" }, Installed());
        var reserved = GameSetCreator.Create(Folders, new NewGameSetRequest { Name = "Empty" }, Installed());

        Assert.Equal("pack_2", second.ModId);
        Assert.Equal(Path.Combine(Folders.GameSets, "Pack_2"), second.Folder);
        Assert.Equal("empty_2", reserved.ModId);
        Assert.Equal(Path.Combine(Folders.GameSets, "Empty_mod"), reserved.Folder);
    }

    [Fact]
    public void Validate_RejectsEmptyNames()
    {
        Assert.NotNull(GameSetCreator.Validate(new NewGameSetRequest { Name = "  " }, Installed()));
        Assert.NotNull(GameSetCreator.Validate(new NewGameSetRequest { Name = "###" }, Installed()));
    }

    [Fact]
    public void GeneralExtension_ExtendsEveryGameSet()
    {
        WriteBaseSets();

        var result = GameSetCreator.Create(Folders, new NewGameSetRequest { Kind = NewGameSetKind.Extension, Name = "Weapons Pack", BaseId = ExtensionTargets.Any }, Installed());

        Assert.True(result.Success, result.Error);
        Assert.Equal(Path.Combine(Folders.Extensions, "Weapons Pack"), result.Folder);
        Assert.Equal("*", Config(result.Folder).GetProperty("Extends").GetString());
        Assert.False(Config(result.Folder).TryGetProperty("MinBaseVersion", out _));
        Assert.Equal("empty-icon", File.ReadAllText(Path.Combine(result.Folder, "icon.png")));
        var installed = Installed();
        Assert.Single(InstalledGameSets.ExtensionsFor(installed, InstalledGameSets.Find(installed, "default")));
    }
}
