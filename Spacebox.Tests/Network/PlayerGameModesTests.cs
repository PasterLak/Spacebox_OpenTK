using Spacebox.Game.Player.GameModes;
using Spacebox.Network.Admin;
using Spacebox.Network.Players;

namespace Spacebox.Tests.Network;

public class PlayerGameModesTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "spacebox-modes-" + Guid.NewGuid().ToString("N"));

    public PlayerGameModesTests()
    {
        Directory.CreateDirectory(_folder);
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
    }

    [Fact]
    public void Modes_PersistAndIgnoreNameCase()
    {
        var modes = new PlayerGameModes();
        modes.Load(_folder);
        Assert.Equal(GameMode.Survival, modes.Get("Anna", GameMode.Survival));

        modes.Set("Anna", GameMode.Creative);
        modes.Set("Bob", GameMode.Spectator);

        var reloaded = new PlayerGameModes();
        reloaded.Load(_folder);
        Assert.Equal(GameMode.Creative, reloaded.Get("anna", GameMode.Survival));
        Assert.Equal(GameMode.Spectator, reloaded.Get(" BOB ", GameMode.Survival));
        Assert.False(reloaded.TryGet("Carl", out _));
    }

    [Fact]
    public void Modes_SkipInvalidValuesInFile()
    {
        File.WriteAllText(Path.Combine(_folder, PlayerGameModes.FileName), "{\"Anna\": 7, \"Bob\": 1}");
        var modes = new PlayerGameModes();
        modes.Load(_folder);

        Assert.False(modes.TryGet("Anna", out _));
        Assert.Equal(GameMode.Creative, modes.Get("Bob", GameMode.Survival));
    }

    [Fact]
    public void Teleport_CoordinatesAreSentForOwnPlayer()
    {
        Assert.Equal(new[] { "Me", "1", "2", "3" }, ServerCommandForwarder.TeleportArguments(new[] { "1", "2", "3" }, "Me"));
        Assert.Equal(new[] { "Bob" }, ServerCommandForwarder.TeleportArguments(new[] { "Bob" }, "Me"));
        Assert.Equal(new[] { "1" }, ServerCommandForwarder.TeleportArguments(new[] { "1" }, "Me"));
    }
}
