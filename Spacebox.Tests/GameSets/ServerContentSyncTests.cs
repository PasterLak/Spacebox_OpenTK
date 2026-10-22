using Spacebox.Game.Resource.Content;
using Spacebox.Game.Resource.TexturePacks;
using Spacebox.Network;

namespace Spacebox.Tests.GameSets;

public sealed class ServerContentSyncTests : IDisposable
{
    private readonly TempGameSets _sets = new TempGameSets();

    public void Dispose() => _sets.Dispose();

    private ModFolders Server => new ModFolders(Path.Combine(_sets.Root, "Server"));

    private void WriteServerMods()
    {
        _sets.Config("Server/GameSets/Default", "default");
        _sets.Write("Server/GameSets/Default/Blocks/stone/block.json", "{ \"ID\": \"stone\" }");
        _sets.Config("Server/Extensions/Lamps", "lamps", "default");
        _sets.Write("Server/Extensions/Lamps/Blocks/lamp/block.json", "{ \"ID\": \"lamp\" }");
        _sets.Write("Server/TexturePacks/Bright/pack.json", "{ \"Id\": \"bright\", \"Name\": \"Bright\" }");
        _sets.Write("Server/TexturePacks/Bright/Blocks/stone.png", "png");
    }

    [Fact]
    public void Resolver_FindsExtensionsInTheExtensionsFolder()
    {
        WriteServerMods();

        var result = GameSetStackResolver.Resolve(Server, "default", new[] { "lamps" });

        Assert.True(result.Success, result.ErrorText);
        Assert.Equal("lamps", Assert.Single(result.Stack.Extensions).Id);
        Assert.Equal(Server.DefaultGameSet, result.Stack.Base.Folder);
    }

    [Fact]
    public void Package_KeepsTheModsLayoutWithExtensionsAndTexturePacks()
    {
        WriteServerMods();
        var stack = GameSetStackResolver.Resolve(Server, "default", new[] { "lamps" }).Stack;
        var packs = TexturePackCatalog.Scan(Server.TexturePacks);
        var world = new RemoteWorldInfo { ModId = "default", Extensions = new[] { "lamps" }, TexturePacks = new[] { "bright" } };

        var client = new ModFolders(Path.Combine(_sets.Root, "Client"));
        Engine.Utils.DirectoryArchive.Extract(GameSetFolders.Pack(stack.Base.Folder, stack.Extensions, packs), Path.Combine(_sets.Root, "Client"));

        Assert.True(Directory.Exists(Path.Combine(client.Extensions, "Lamps")));
        Assert.True(GameSetDownloader.TryResolve(client, world, out var downloaded, out var downloadedPacks));
        Assert.Equal("bright", Assert.Single(downloadedPacks).Id);
        Assert.Equal(GameSetFolders.ComputeHash(stack, packs), GameSetFolders.ComputeHash(downloaded, downloadedPacks));
    }

    [Fact]
    public void Hash_ChangesWithServerTexturePacks()
    {
        WriteServerMods();
        var stack = GameSetStackResolver.Resolve(Server, "default", new[] { "lamps" }).Stack;
        var packs = TexturePackCatalog.Scan(Server.TexturePacks);

        string withPack = GameSetFolders.ComputeHash(stack, packs);
        Assert.NotEqual(GameSetFolders.ComputeHash(stack), withPack);

        _sets.Write("Server/TexturePacks/Bright/Blocks/stone.png", "changed");
        Assert.NotEqual(withPack, GameSetFolders.ComputeHash(stack, TexturePackCatalog.Scan(Server.TexturePacks)));
    }

    [Fact]
    public void Resolve_FailsWhenAServerTexturePackIsMissing()
    {
        WriteServerMods();
        var world = new RemoteWorldInfo { ModId = "default", Extensions = new[] { "lamps" }, TexturePacks = new[] { "bright", "dark" } };

        Assert.False(GameSetDownloader.TryResolve(Server, world, out _, out _));
    }

    [Fact]
    public void SessionPacks_AddServerPacksBelowTheClientsOwn()
    {
        WriteServerMods();
        _sets.Write("Client/TexturePacks/Mine/pack.json", "{ \"Id\": \"mine\" }");
        var own = TexturePackCatalog.Scan(Path.Combine(_sets.Root, "Client", "TexturePacks"));
        var server = TexturePackCatalog.Scan(Server.TexturePacks);

        var active = TexturePackCatalog.WithSessionPacks(new List<TexturePack>(own), server.Concat(own).ToList());

        Assert.Equal(new[] { "mine", "bright" }, active.Select(p => p.Id));
    }
}
