using OpenTK.Mathematics;

using Engine.Commands;
using Spacebox.Game.Player;
using Engine;

namespace Spacebox.Game.Commands
{
    internal class ClearInventoryCommand : CommandBase
    {
        public override string Name => "clear_inventory";

        public override string Description => "Delete all items from the inventory";

        public LocalAstronaut Astronaut { get; set; }


        public ClearInventoryCommand(LocalAstronaut astronaut)
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

            if (args.Length == 0)
            {
                Astronaut.Panel.Clear();
                Astronaut.Inventory.Clear();

                Debug.Success("Inventory cleared!");
            }


        }


    }
}
