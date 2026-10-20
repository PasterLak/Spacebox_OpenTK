using System.Text.Json;
using Engine.Networking;
using Spacebox.Network;
using Spacebox.Network.Admin;
using Spacebox.Network.Dedicated;

namespace Spacebox.Tests.Network;

public class ServerAdminsTests
{
    private static ServerConfig Config(bool registration, bool show, params string[] admins)
    {
        return new ServerConfig { RegistrationEnabled = registration, ShowAdmins = show, Admins = new List<string>(admins) };
    }

    [Fact]
    public void Defaults_NoAdminsAndShowAdminsOn()
    {
        var config = new ServerConfig();

        Assert.Empty(config.Admins);
        Assert.True(config.ShowAdmins);
        Assert.True(JsonSerializer.Deserialize<ServerConfig>("{ \"MaxPlayers\": 4 }")!.ShowAdmins);
    }

    [Fact]
    public void Contains_IgnoresCaseAndSpaces()
    {
        var admins = new List<string> { " Vlad " };

        Assert.True(ServerAdmins.Contains(admins, "vlad"));
        Assert.True(ServerAdmins.Contains(admins, "VLAD "));
        Assert.False(ServerAdmins.Contains(admins, "vlad2"));
        Assert.False(ServerAdmins.Contains(admins, ""));
        Assert.False(ServerAdmins.Contains(null, "vlad"));
    }

    [Fact]
    public void AdminRights_NeedRegistration()
    {
        Assert.True(ServerAdmins.IsAdmin(Config(true, true, "Vlad"), "vlad"));
        Assert.False(ServerAdmins.IsAdmin(Config(false, true, "Vlad"), "vlad"));
        Assert.False(ServerAdmins.IsAdmin(Config(true, true, "Vlad"), "Other"));
        Assert.False(ServerAdmins.IsAdmin(null, "vlad"));
    }

    [Fact]
    public void Badge_FollowsShowAdmins()
    {
        Assert.True(ServerAdmins.ShowsBadge(Config(true, true, "Vlad"), "Vlad"));
        Assert.False(ServerAdmins.ShowsBadge(Config(true, false, "Vlad"), "Vlad"));
        Assert.False(ServerAdmins.ShowsBadge(Config(false, true, "Vlad"), "Vlad"));
        Assert.Equal("[ADMIN] Vlad", ServerAdmins.Label("Vlad", true));
        Assert.Equal("Vlad", ServerAdmins.Label("Vlad", false));
    }

    [Fact]
    public void TryAdd_TrimsAndSkipsDuplicates()
    {
        var admins = new List<string>();

        Assert.True(ServerAdmins.TryAdd(admins, " Vlad ", out _));
        Assert.True(ServerAdmins.TryAdd(admins, "VLAD", out _));
        Assert.Equal(new[] { "Vlad" }, admins);
        Assert.False(ServerAdmins.TryAdd(admins, "  ", out var error));
        Assert.NotNull(error);
        Assert.False(ServerAdmins.TryAdd(admins, new string('a', ServerAdmins.MaxNameLength + 1), out _));
    }

    [Fact]
    public void Remove_IgnoresCase()
    {
        var admins = new List<string> { "Vlad", "Anna" };

        Assert.True(ServerAdmins.Remove(admins, "vlad"));
        Assert.False(ServerAdmins.Remove(admins, "vlad"));
        Assert.Equal(new[] { "Anna" }, admins);
    }

    [Theory]
    [InlineData("/gm 1", true, "gm 1")]
    [InlineData("/kick  Bob ", true, "kick  Bob")]
    [InlineData("/", false, "")]
    [InlineData("/ gm", false, "")]
    [InlineData("hello /gm", false, "")]
    [InlineData("", false, "")]
    public void ChatCommands_AreDetected(string text, bool isCommand, string line)
    {
        Assert.Equal(isCommand, ChatCommands.IsCommand(text));
        Assert.Equal(line, ChatCommands.ToCommandLine(text));
    }

    [Fact]
    public void ChatEcho_IsNotSentBackAsCommandOutput()
    {
        Assert.True(ChatCommands.IsEchoOfChat("[Chat] [Server]: hi"));
        Assert.False(ChatCommands.IsEchoOfChat("[Success] Vlad: game mode Creative"));
    }

    [Fact]
    public void PlayerList_KeepsTheAdminFlag()
    {
        var message = new PlayerListMessage
        {
            Players = new List<PlayerListEntry>
            {
                new PlayerListEntry { Peer = 2, Name = "Vlad", Ping = 30, IsAdmin = true },
                new PlayerListEntry { Peer = 3, Name = "Anna", Ping = 40, IsMuted = true }
            }
        };
        var writer = new NetWriter();
        message.Serialize(writer);

        var copy = new PlayerListMessage();
        copy.Deserialize(new NetReader(writer.ToArray()));

        Assert.True(copy.Players[0].IsAdmin);
        Assert.False(copy.Players[1].IsAdmin);
        Assert.True(copy.Players[1].IsMuted);
        Assert.Equal("Anna", copy.Players[1].Name);
    }
}
