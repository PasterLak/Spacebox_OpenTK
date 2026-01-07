
using Engine.Commands;
using Spacebox.Game.Player;
using Engine;
using System.Globalization;
using System.Numerics;
using Spacebox.Game.Generation;

namespace Spacebox.Game.Commands
{
    internal class TeleportLocal : CommandBase
    {
        public override string Name => "tpl";

        public override string Description => "Teleport the player in local sector coords (0-8191). Usage: tp x y z";

        public LocalAstronaut Astronaut { get; set; }


        public TeleportLocal(LocalAstronaut astronaut)
        {
            this.Astronaut = astronaut;
        }
        public override void Execute(string[] args)
        {
            if (args.Length < 3)
            {
                Debug.Error($"Usage: {Name} [x] [y] [z]");
                return;
            }

            if (Astronaut == null)
            {
                Debug.Error("Astronaut reference is null.");
                return;
            }

            if (TryParseArguments(args, out float x, out float y, out float z))
            {
               var world =  World.CurrentSector.LocalToWorldPosition(new OpenTK.Mathematics.Vector3(x,y,z));
                Astronaut.Teleport(world);
                Debug.Log($"Teleported to: ({x}, {y}, {z})", new Vector4(1f, 1f, 0f, 1f));
            }
            else
            {
                Debug.Error("Invalid arguments. Please enter valid numbers for x, y, and z.");
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
