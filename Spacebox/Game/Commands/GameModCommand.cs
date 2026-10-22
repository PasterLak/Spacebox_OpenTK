
using Engine.Commands;
using Spacebox.Game.Player;
using Engine;
using Spacebox.Game.Player.GameModes;

namespace Spacebox.Game.Commands
{
    internal class GameModCommand : CommandBase, Engine.Commands.IArgumentSuggestions
    {
        public void SuggestArguments(int argumentIndex, IReadOnlyList<string> previousArguments, List<string> results)
        {
            if (argumentIndex == 0) Spacebox.Game.Commands.CommandSuggestionSources.AddGameModes(results);
            else if (argumentIndex == 1) Spacebox.Game.Commands.CommandSuggestionSources.AddPlayerNames(results);
        }

        public override string Name => "gm";

        public override string Description => "Change gamemode. Usage: gm <0,1,2> [player]  (survival,creative,spectator; on a server only admins)";

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

            if (args.Length >= 2 && Engine.Networking.NetworkManager.Instance.IsServer)
            {
                new Spacebox.Network.Dedicated.ServerGameModeCommand().Execute(args);
                return;
            }

            if (ValidateArgs(args, 1) == false) return;



            if (TryParse<int>(args[0], out var id))
            {
                GameMode gm = GameMode.Spectator;
                if (id == 0)
                {
                    gm = GameMode.Survival;
                }
                if (id == 1)
                {
                    gm = GameMode.Creative;
                }
                if (id == 2)
                {
                    gm = GameMode.Spectator;
                }

                if (id >= 0 && id <= 2)
                {
                    Astronaut.GameMode = gm;
                    Debug.Success("Gamemod changed to " + gm.ToString());
                }
                else
                {
                    Debug.Error("Wrong game mode id!");
                }

            }





        }


    }
}