using Spacebox.Network.Dedicated;

namespace Spacebox.Tests.Network;

public sealed class ServerConfigLoadTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "spacebox_config_" + Guid.NewGuid().ToString("N"));

    public ServerConfigLoadTests() => Directory.CreateDirectory(_root);

    public void Dispose() => Directory.Delete(_root, true);

    [Fact]
    public void BrokenFile_IsNotOverwritten_AndACopyIsKept()
    {
        string path = Path.Combine(_root, ServerConfig.FileName);
        const string broken = "{ \"Admins\": [\"Alice\"], \"WhitelistEnabled\": true, ";
        File.WriteAllText(path, broken);

        var config = ServerConfig.LoadOrCreate(path);

        Assert.NotNull(config);
        Assert.Empty(config.Admins);
        Assert.Equal(broken, File.ReadAllText(path));
        Assert.Equal(broken, File.ReadAllText(path + ServerConfig.BrokenSuffix));
    }

    [Fact]
    public void NullLists_GetDefaults()
    {
        string path = Path.Combine(_root, ServerConfig.FileName);
        File.WriteAllText(path, "{ \"Admins\": null, \"Whitelist\": null, \"Backups\": null, \"TexturePacks\": null, \"Claims\": null, \"Gameplay\": null }");

        var config = ServerConfig.LoadOrCreate(path);

        Assert.NotNull(config.Admins);
        Assert.NotNull(config.Whitelist);
        Assert.NotNull(config.Backups);
        Assert.NotNull(config.TexturePacks);
        Assert.NotNull(config.Claims);
        Assert.NotNull(config.Gameplay);
        Assert.False(File.Exists(path + ServerConfig.BrokenSuffix));
    }

    [Fact]
    public void ValidFile_KeepsItsValues()
    {
        string path = Path.Combine(_root, ServerConfig.FileName);
        File.WriteAllText(path, "{ \"Admins\": [\"Alice\"], \"WhitelistEnabled\": true, \"Whitelist\": [\"Bob\"] }");

        var config = ServerConfig.LoadOrCreate(path);

        Assert.Equal(new[] { "Alice" }, config.Admins);
        Assert.True(config.WhitelistEnabled);
        Assert.Equal(new[] { "Bob" }, config.Whitelist);
    }

    [Fact]
    public void NumericSeed_IsReadAsText()
    {
        string path = Path.Combine(_root, ServerConfig.FileName);
        File.WriteAllText(path, "{ \"Seed\": 12345, \"Port\": 7791 }");

        var config = ServerConfig.LoadOrCreate(path);

        Assert.Equal("12345", config.Seed);
        Assert.Equal(7791, config.Port);
        Assert.False(File.Exists(path + ServerConfig.BrokenSuffix));
        Assert.Equal("12345", ServerConfig.LoadOrCreate(path).Seed);
    }

    [Fact]
    public void HandEditedFile_WithCommentsTrailingCommasAndLowerCaseNames_Loads()
    {
        string path = Path.Combine(_root, ServerConfig.FileName);
        File.WriteAllText(path, "{\n  // my server\n  \"port\": 7800,\n  \"serverName\": \"Home\",\n  \"Admins\": [\"Alice\",],\n}");

        var config = ServerConfig.LoadOrCreate(path);

        Assert.Equal(7800, config.Port);
        Assert.Equal("Home", config.ServerName);
        Assert.Equal(new[] { "Alice" }, config.Admins);
        Assert.False(File.Exists(path + ServerConfig.BrokenSuffix));
    }
}
