
using Engine.Commands;
using Spacebox.Game.Player;
using Engine;
using System.Numerics;
using Client;

namespace Spacebox.Game.Commands
{
    internal class TeleportToPlayerCommand : CommandBase
    {
        public override string Name => "tpp";

        public override string Description => "Teleport to a player. Usage: tpp <id>";

        public Astronaut Astronaut { get; set; }


        public TeleportToPlayerCommand(Astronaut astronaut)
        {
            this.Astronaut = astronaut;
        }

        public override void Execute(string[] args)
        {
            if (args.Length < 1)
            {
                Debug.Error($"Usage: {Name} [playerID]");
                return;
            }

            if (Astronaut == null)
            {
                Debug.Error("Astronaut reference is null.");
                return;
            }

            if (!Is<int>(args[0]))
            {
                Debug.Error("Enter the player ID! ");
                return;
            }
            var id = int.Parse(args[0]);
            if (ClientNetwork.Instance != null)
            {
                var players = ClientNetwork.Instance.GetClientPlayers();

                ClientPlayer player = null;

                foreach (var p in players)
                {
                    if (p.NetworkPlayer.ID == id)
                    {
                        player = p;
                        break;
                    }
                }

                if (player != null)
                {
                 
                    Astronaut.Teleport(player.RemotePlayer.Position);
                    Debug.Log("Teleported to " + player.NetworkPlayer.Name, new Vector4(0f, 1f, 0f, 1f));
                }
                else
                {
                    Debug.Error("There are no player with ID: " + id);
                }
            }
            else
            {
                Debug.Error("There are no ClientNetwork.Instance ");
            }
        }

    }
}
