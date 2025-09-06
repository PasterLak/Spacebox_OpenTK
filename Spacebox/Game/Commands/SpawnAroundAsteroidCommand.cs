using OpenTK.Mathematics;

using Engine.Commands;
using Spacebox.Game.Generation;
using Spacebox.Game.Player;
using Engine;

namespace Spacebox.Game.Commands
{
    internal class SpawnAroundAsteroidCommand : ICommand
    {
        public string Name => "tpa";

        public string Description => "tp to asteroid";

        public Astronaut Astronaut { get; set; }


        public SpawnAroundAsteroidCommand(Astronaut astronaut)
        {
            this.Astronaut = astronaut;
        }
        public void Execute(string[] args)
        {


            if (Astronaut == null)
            {
                Debug.Error("Astronaut reference is null.");
                return;
            }

            if (args.Length == 0)
            {

                var sector = World.CurrentSector;

                if(sector.TryGetNearestEntity(Astronaut.Position, out var entity))
                {
                    var pos = sector.GetRandomPositionNearAsteroid(new Random(), entity);

                    Astronaut.Position = pos;
                }

                
            }


        }


    }
}