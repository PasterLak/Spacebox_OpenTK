using Engine.Commands;
using Spacebox.Game.Generation;
using Spacebox.Game.Player;
using Spacebox.Game.Player.Respawn;
using Engine;

namespace Spacebox.Game.Commands
{
    internal class SpawnAroundAsteroidCommand : CommandBase
    {
        public override string Name => "tpa";

        public override string Description => "Teleport to the nearest asteroid (the next one if you are already at one)";
        public override bool AvailableInTerminal => false;

        public LocalAstronaut Astronaut { get; set; }

        public SpawnAroundAsteroidCommand(LocalAstronaut astronaut)
        {
            this.Astronaut = astronaut;
        }

        public override void Execute(string[] args)
        {
            if (Spacebox.Network.Admin.ChatCommands.RefusesRemoteSender(Name)) return;
            if (Astronaut == null)
            {
                Debug.Error("Astronaut reference is null.");
                return;
            }

            if (args.Length > 0)
            {
                Debug.Error($"Usage: {Name}");
                return;
            }

            var sector = World.CurrentSector;
            if (sector == null || !sector.TryFindNearestAsteroid(Astronaut.Position, out var target, out var loaded))
            {
                Debug.Warning("No asteroid found in this sector");
                return;
            }

            var position = AsteroidApproach.ApproachPoint(target, Astronaut.Position);
            if (loaded != null) position = SafeSpawnFinder.FindFreePosition(position);

            float distance = (position - Astronaut.Position).Length;
            Astronaut.Teleport(position);
            Debug.Success($"Teleported {distance:0} m to an asteroid");
        }
    }
}
