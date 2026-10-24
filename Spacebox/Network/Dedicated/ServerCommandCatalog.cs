using Engine.Commands;

namespace Spacebox.Network.Dedicated;

public static class ServerCommandCatalog
{
    private static CommandBase[] _reference;

    public static CommandBase[] Create()
    {
        return new CommandBase[]
        {
            new SayCommand(),
            new PlayersCommand(),
            new PlayersCommand("list"),
            new WhoisCommand(),
            new KickCommand(),
            new KickAllCommand(),
            new BanCommand(),
            new UnbanCommand(),
            new UnregisterCommand(),
            new MuteCommand(true),
            new MuteCommand(false),
            new SaveCommand(),
            new StatsCommand(),
            new StopCommand(),
            new RestartCommand(),
            new ServerGameModeCommand()
        };
    }

    public static IReadOnlyList<CommandBase> Reference => _reference ??= Create();
}
