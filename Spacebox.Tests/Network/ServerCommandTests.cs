using Engine;
using Engine.Commands;
using OpenTK.Mathematics;
using Spacebox.Game.Commands;
using Spacebox.Network;
using Spacebox.Network.Admin;
using Spacebox.Network.Dedicated;

namespace Spacebox.Tests.Network;

[Collection(Spacebox.Tests.GameAssetsCollection.Name)]
public class ServerCommandTests
{
    public static IEnumerable<object[]> CommandsNeedingArguments()
    {
        yield return new object[] { new SayCommand() };
        yield return new object[] { new KickCommand() };
        yield return new object[] { new BanCommand() };
        yield return new object[] { new UnbanCommand() };
        yield return new object[] { new MuteCommand(true) };
        yield return new object[] { new MuteCommand(false) };
        yield return new object[] { new UnregisterCommand() };
        yield return new object[] { new WhoisCommand() };
    }

    [Theory]
    [MemberData(nameof(CommandsNeedingArguments))]
    public void CommandWithoutArguments_PrintsItsUsage(CommandBase command)
    {
        var output = CommandCapture.Run(command);

        Assert.Contains(output, line => line.Type == Debug.MessageType.Error && line.Text.Contains("Usage: " + command.Name + " ", StringComparison.Ordinal));
    }

    [Fact]
    public void Catalog_HasUniqueNamesAndTheNewCommands()
    {
        var names = ServerCommandCatalog.Create().Select(command => command.Name).ToList();

        Assert.Equal(names.Count, names.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Contains("whois", names);
        Assert.Contains("kickall", names);
        Assert.Contains("list", names);
        Assert.All(ServerCommandCatalog.Create(), command => Assert.False(string.IsNullOrWhiteSpace(command.Description)));
        Assert.All(ServerCommandCatalog.Create(), command => Assert.True(command.AvailableInTerminal));
    }

    [Fact]
    public void ListCommand_IsAnAliasOfPlayers()
    {
        var list = new PlayersCommand("list");

        Assert.Equal("list", list.Name);
        Assert.Equal(new PlayersCommand().Description, list.Description);
        Assert.StartsWith("Players online:", CommandCapture.Run(list)[0].Text);
    }

    [Fact]
    public void WhoisReport_ListsEveryDetail()
    {
        var lines = WhoisReport.Lines(new WhoisInfo("Anna", 3, "192.168.1.20", 42, "Creative", new Vector3(1.26f, -2f, 300f), true, true, true, TimeSpan.FromMinutes(75)));

        Assert.Equal("Anna [admin, muted]", lines[0]);
        Assert.Contains("Peer 3, address 192.168.1.20, ping 42 ms", lines[1]);
        Assert.Contains("Creative", lines[2]);
        Assert.Contains("1.3 -2.0 300.0", lines[3]);
        Assert.Contains("1 h 15 min", lines[4]);
    }

    [Fact]
    public void WhoisReport_ShowsDeadAndUnspawnedPlayers()
    {
        var lines = WhoisReport.Lines(new WhoisInfo("Bob", 4, "", 0, "Survival", null, false, false, false, TimeSpan.FromSeconds(65)));

        Assert.Equal("Bob [dead]", lines[0]);
        Assert.Contains("address unknown", lines[1]);
        Assert.Contains("not spawned yet", lines[3]);
        Assert.Contains("1 min 5 s", lines[4]);
    }

    [Fact]
    public void KickAll_SkipsTheHostAndTheAdminWhoAsked()
    {
        var players = new[]
        {
            new ConnectedPlayer { Peer = 0, Name = "Host" },
            new ConnectedPlayer { Peer = 1, Name = "Anna" },
            new ConnectedPlayer { Peer = 2, Name = "Bob" }
        };

        var targets = KickAllCommand.Select(players, " anna ", 0);

        Assert.Equal(new[] { "Bob" }, targets.Select(player => player.Name));
        Assert.Equal(2, KickAllCommand.Select(players, null, 0).Count);
    }

    [Fact]
    public void KickAll_WithNobodyOnline_SaysSo()
    {
        var output = CommandCapture.Run(new KickAllCommand());

        Assert.Contains(output, line => line.Type == Debug.MessageType.Warning && line.Text.EndsWith("Nobody to kick", StringComparison.Ordinal));
    }

    [Fact]
    public void Seed_DescribesTheWorld()
    {
        Assert.Equal("World 'Home', seed 123, GameSet default", SeedCommand.Describe("Home", "123", "default"));
        Assert.Equal("World 'Home', seed unknown, GameSet unknown", SeedCommand.Describe("Home", "", null));
    }

    [Fact]
    public void GameRule_DescriptionAndSuggestionsCoverEveryRule()
    {
        var command = new GameRuleCommand();
        var suggestions = new List<string>();
        command.SuggestArguments(0, Array.Empty<string>(), suggestions);

        foreach (var rule in ServerRulesService.Rules)
        {
            Assert.Contains(rule.Name, command.Description);
            Assert.Contains(rule.Name, suggestions);
        }

        var values = new List<string>();
        command.SuggestArguments(1, new[] { "pvp" }, values);
        Assert.Equal(new[] { "on", "off" }, values);

        values.Clear();
        command.SuggestArguments(1, new[] { "death_drop_minutes" }, values);
        Assert.Empty(values);
    }

    private sealed class LocalOnlyCommand : CommandBase
    {
        public int Runs;
        public override string Name => "localonlytestcommand";
        public override string Description => "test";
        public override bool AvailableInTerminal => false;
        public override void Execute(string[] args) => Runs++;
    }

    private sealed class RemoteAwareCommand : CommandBase, IRemoteSenderCommand
    {
        public override string Name => "remoteawaretestcommand";
        public override string Description => "test";
        public override bool AvailableInTerminal => false;
        public override void Execute(string[] args) => Debug.Log("ran");
    }

    [Fact]
    public void ChatCommands_RefuseLocalOnlyCommandsFromOtherPlayers()
    {
        var command = CommandManager.GetCommand("localonlytestcommand") as LocalOnlyCommand;
        if (command == null)
        {
            command = new LocalOnlyCommand();
            CommandManager.RegisterGlobalCommand(command);
        }
        int before = command.Runs;

        var output = ChatCommands.Run("localonlytestcommand", "SomeoneElse");

        Assert.Equal(before, command.Runs);
        Assert.Contains(output, line => line.Type == Debug.MessageType.Error && line.Text.Contains("only for the host's own player"));

        ChatCommands.Run("localonlytestcommand", GameSession.PlayerName);
        Assert.Equal(before + 1, command.Runs);
    }

    [Fact]
    public void ChatCommands_LetRemoteAwareCommandsRunForOtherPlayers()
    {
        if (CommandManager.GetCommand("remoteawaretestcommand") == null) CommandManager.RegisterGlobalCommand(new RemoteAwareCommand());

        var output = ChatCommands.Run("remoteawaretestcommand", "SomeoneElse");

        Assert.Contains(output, line => line.Text == "ran");
        Assert.True(ChatCommands.AcceptsSender(null));
    }
}
