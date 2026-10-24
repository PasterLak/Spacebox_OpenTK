
using Engine.Commands;
using Spacebox.Game.Player;
using Engine;
using Spacebox.Game.Player.GameModes;

namespace Spacebox.Game.Commands
{
    internal class GameModCommand : CommandBase, Engine.Commands.IArgumentSuggestions, Spacebox.Network.Admin.IRemoteSenderCommand
    {
        public void SuggestArguments(int argumentIndex, IReadOnlyList<string> previousArguments, List<string> results)
        {
            if (argumentIndex == 0) Spacebox.Game.Commands.CommandSuggestionSources.AddGameModes(results);
            else if (argumentIndex == 1) Spacebox.Game.Commands.CommandSuggestionSources.AddPlayerNames(results);
        }

        public override string Name => "gm";

        public override string Description => "Change gamemode. Usage: gm <mode> [player]  (0 survival, 1 creative, 2 spectator or a GameSet mode by id or name; on a server only admins)";

        public override bool AvailableInTerminal => false;

        public LocalAstronaut Astronaut { get; set; }


        public GameModCommand(LocalAstronaut astronaut)
        {
            this.Astronaut = astronaut;
        }
        public override void Execute(string[] args)
        {


            if (Astronaut == null)
            {
                Debug.Error("Astronaut reference is null.");
                return;
            }

            if (Spacebox.Network.Admin.ServerCommandForwarder.TryForward(Name, args)) return;

            if ((args.Length >= 2 || Spacebox.Network.Admin.ChatCommands.RemoteSender != null) && Engine.Networking.NetworkManager.Instance.IsServer)
            {
                new Spacebox.Network.Dedicated.ServerGameModeCommand().Execute(args);
                return;
            }

            if (ValidateArgs(args, 1) == false) return;



            if (!GameModeCatalog.TryFind(args[0], out var definition))
            {
                Debug.Error($"Unknown game mode. Modes: {GameModeList.Describe()}");
                return;
            }

            Astronaut.GameMode = definition.Id;
            Debug.Success("Game mode changed to " + definition.Name);
        }


    }
}