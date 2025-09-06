
using Engine.Commands;
using Spacebox.Game.Player;
using Engine;
using System.Globalization;
using System.Numerics;
using Client;

namespace Spacebox.Game.Commands
{
    internal class TeleportToPlayerCommand : ICommand
    {
        public string Name => "tpp";

        public string Description => "teleport to a player";

        public Astronaut Astronaut { get; set; }


        public TeleportToPlayerCommand(Astronaut astronaut)
        {
            this.Astronaut = astronaut;
        }

        public bool IsNumber(string text)
        {
            return int.TryParse(text, out _);
        }

        public void Execute(string[] args)
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

            if (!IsNumber(args[0]))
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
                    Astronaut.Position = player.RemotePlayer.Position;
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

        private bool TryParseArguments(string[] args, out float x, out float y, out float z)
        {
            bool isXValid = float.TryParse(args[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x);
            bool isYValid = float.TryParse(args[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y);
            bool isZValid = float.TryParse(args[2], NumberStyles.Float, CultureInfo.InvariantCulture, out z);

            return isXValid && isYValid && isZValid;
        }
    }
}
