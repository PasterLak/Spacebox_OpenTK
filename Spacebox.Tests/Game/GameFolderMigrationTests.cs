using Spacebox.Game.Resource;

namespace Spacebox.Tests.Game;

public sealed class GameFolderMigrationTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "spacebox_folders_" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    private string Write(string relative, string content = "x")
    {
        string path = Path.Combine(_root, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    private bool Exists(string relative) => File.Exists(Path.Combine(_root, relative.Replace('/', Path.DirectorySeparatorChar)));

    private string Read(string relative) => File.ReadAllText(Path.Combine(_root, relative.Replace('/', Path.DirectorySeparatorChar)));

    [Fact]
    public void GameSetsAndExtensionsAndTexturePacks_MoveIntoMods()
    {
        Write("GameSets/MyMod/config.json", "{ \"ModId\": \"my\" }");
        Write("GameSets/MyExtension/config.json", "{ \"ModId\": \"ext\", \"Extends\": \"default\" }");
        Write("GameSets/new.zip");
        Write("TexturePacks/Pack/pack.json", "{}");

        GameFolderMigration.Run(_root);

        Assert.True(Exists("Mods/GameSets/MyMod/config.json"));
        Assert.True(Exists("Mods/Extensions/MyExtension/config.json"));
        Assert.True(Exists("Mods/GameSets/new.zip"));
        Assert.True(Exists("Mods/TexturePacks/Pack/pack.json"));
        Assert.False(Directory.Exists(Path.Combine(_root, "GameSets")));
        Assert.False(Directory.Exists(Path.Combine(_root, "TexturePacks")));
    }

    [Fact]
    public void ExistingTargets_AreNeverOverwritten()
    {
        Write("GameSets/Default/config.json", "old");
        Write("Mods/GameSets/Default/config.json", "new");

        GameFolderMigration.Run(_root);

        Assert.Equal("new", Read("Mods/GameSets/Default/config.json"));
        Assert.Equal("old", Read("GameSets.old/Default/config.json"));
        Assert.Empty(GameFolderMigration.Run(_root));
    }

    [Fact]
    public void MultiplayerFiles_MoveIntoTheMultiplayerFolder()
    {
        Write("client.json", "client");
        Write("serverconfig.json", "mine");
        Write("Multiplayer/serverconfig.json", "shipped");
        Write("server-icon.png");
        Write("dashboard/admin.json");
        Write("Multiplayer/credentials.json");
        Write("Multiplayer/ServerIcons/a.png");
        Write("Multiplayer/127.0.0.1_7777/World/world.json");
        Write("Server/127.0.0.1_7777/GameSet/Default/config.json");
        Write("command_history.txt");

        GameFolderMigration.Run(_root);

        Assert.Equal("client", Read("Multiplayer/client.json"));
        Assert.Equal("mine", Read("Multiplayer/serverconfig.json"));
        Assert.True(Exists("Multiplayer/server-icon.png"));
        Assert.True(Exists("Multiplayer/Dashboard/admin.json"));
        Assert.True(Exists("Multiplayer/credentials.json"));
        Assert.True(Exists("Multiplayer/ServerIcons/a.png"));
        Assert.True(Exists("Multiplayer/Worlds/127.0.0.1_7777/World/world.json"));
        Assert.False(Directory.Exists(Path.Combine(_root, "Server")));
        Assert.True(Exists("Debug/command_history.txt"));
        Assert.False(Exists("client.json") || Exists("serverconfig.json") || Exists("command_history.txt"));
    }

    [Fact]
    public void EmptyFolderTrees_AreRemovedInsteadOfMoved()
    {
        Directory.CreateDirectory(Path.Combine(_root, "GameSets", "Stale", "Blocks", "lamp"));

        GameFolderMigration.Run(_root);

        Assert.False(Directory.Exists(Path.Combine(_root, "Mods", "GameSets", "Stale")));
        Assert.False(Directory.Exists(Path.Combine(_root, "GameSets")));
    }

    [Fact]
    public void SecondRun_ChangesNothing()
    {
        Write("GameSets/MyMod/config.json", "{ \"ModId\": \"my\" }");
        GameFolderMigration.Run(_root);

        Assert.Empty(GameFolderMigration.Run(_root));
    }
}
