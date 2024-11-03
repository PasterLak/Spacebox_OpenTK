using Spacebox.Common;
using Spacebox.Common.Commands;
using System;
using System.Globalization;
using System.Numerics;

namespace Spacebox.Game.Commands
{
    internal class TeleportCommand : ICommand
    {
        public string Name => "tp";

        public string Description => "teleport the player";

        public Astronaut Astronaut { get; set; }

    
        public TeleportCommand(Astronaut astronaut) {
            this.Astronaut = astronaut;
        }
        public void Execute(string[] args)
        {
            if (args.Length < 3)
            {
                GameConsole.AddMessage($"Usage: {Name} [x] [y] [z]", new Vector4(1f, 0f, 0f, 1f));
                return;
            }

            if (Astronaut == null)
            {
                GameConsole.AddMessage("Astronaut reference is null.", new Vector4(1f, 0f, 0f, 1f));
                return;
            }

            if (TryParseArguments(args, out float x, out float y, out float z))
            {
                Astronaut.Position = new OpenTK.Mathematics.Vector3(x, y, z);
                GameConsole.AddMessage($"Teleported to: ({x}, {y}, {z})", new Vector4(1f, 1f, 0f, 1f));
            }
            else
            {
                GameConsole.AddMessage("Invalid arguments. Please enter valid numbers for x, y, and z.", new Vector4(1f, 0f, 0f, 1f));
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
