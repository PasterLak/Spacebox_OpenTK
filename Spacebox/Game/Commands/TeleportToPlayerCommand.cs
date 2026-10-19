
using Engine.Commands;
using Spacebox.Game.Player;
using Engine;
using Engine.SceneManagement;
using System.Numerics;

namespace Spacebox.Game.Commands
{
    internal class TeleportToPlayerCommand : CommandBase
    {
        public override string Name => "tpp";

        public override string Description => "Teleport to a player. Usage: tpp <name>";
        public override bool AvailableInTerminal => false;

        public LocalAstronaut Astronaut { get; set; }


        public TeleportToPlayerCommand(LocalAstronaut astronaut)
        {
            this.Astronaut = astronaut;
        }

        public override void Execute(string[] args)
        {
            if (args.Length < 1)
            {
                Debug.Error($"Usage: {Name} [playerName]");
                return;
            }

            if (Astronaut == null)
            {
                Debug.Error("Astronaut reference is null.");
                return;
            }

            var target = FindPlayer(SceneManager.Current, args[0]);
            if (target == null)
            {
                Debug.Error("There is no player named " + args[0]);
                return;
            }

            Astronaut.Teleport(target.Position);
            Debug.Log("Teleported to " + target.PlayerName, new Vector4(0f, 1f, 0f, 1f));
        }

        private static RemoteAstronaut FindPlayer(Node3D node, string name)
        {
            if (node == null) return null;
            if (node is RemoteAstronaut remote && string.Equals(remote.PlayerName, name, StringComparison.OrdinalIgnoreCase)) return remote;

            for (int i = 0; i < node.Children.Count; i++)
            {
                var found = FindPlayer(node.Children[i], name);
                if (found != null) return found;
            }

            return null;
        }
    }
}
