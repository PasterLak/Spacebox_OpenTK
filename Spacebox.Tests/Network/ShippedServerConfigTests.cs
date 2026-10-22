using System.Text.Json;
using Spacebox.Network.Dedicated;

namespace Spacebox.Tests.Network;

public class ShippedServerConfigTests
{
    [Fact]
    public void ShippedConfig_ContainsEverySetting()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(FindRepoFile(Path.Combine("Spacebox", "Multiplayer", "serverconfig.json"))));
        var root = document.RootElement;

        foreach (var property in typeof(ServerConfig).GetProperties())
        {
            if (!property.CanWrite) continue;
            Assert.True(root.TryGetProperty(property.Name, out _), $"serverconfig.json misses {property.Name}");
        }
    }

    [Fact]
    public void ShippedConfig_UsesTheDefaults()
    {
        var shipped = JsonSerializer.Deserialize<ServerConfig>(File.ReadAllText(FindRepoFile(Path.Combine("Spacebox", "Multiplayer", "serverconfig.json"))))!;
        var defaults = new ServerConfig();

        Assert.False(shipped.FriendlyMobs);
        Assert.Equal(defaults.Port, shipped.Port);
        Assert.Equal(defaults.MaxPlayers, shipped.MaxPlayers);
        Assert.Equal(defaults.UpdateRate, shipped.UpdateRate);
    }

    [Fact]
    public void ShippedConfig_MatchesANewlyCreatedConfig()
    {
        var shipped = JsonSerializer.Deserialize<ServerConfig>(File.ReadAllText(FindRepoFile(Path.Combine("Spacebox", "Multiplayer", "serverconfig.json"))))!;
        var created = ServerConfig.CreateNew();

        Assert.Equal(created.RegistrationEnabled, shipped.RegistrationEnabled);
        Assert.Equal(created.GameMode, shipped.GameMode);
    }

    private static string FindRepoFile(string relative)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, relative);
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new FileNotFoundException(relative);
    }
}
