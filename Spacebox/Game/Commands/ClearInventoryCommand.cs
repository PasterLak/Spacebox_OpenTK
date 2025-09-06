using OpenTK.Mathematics;

using Engine.Commands;
using Spacebox.Game.Player;
using Engine;

namespace Spacebox.Game.Commands
{
    internal class ClearInventoryCommand : ICommand
    {
        public string Name => "clear_inventory";

        public string Description => "tag <create/delete> <name>";

        public Astronaut Astronaut { get; set; }


        public ClearInventoryCommand(Astronaut astronaut)
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
                Astronaut.Panel.Clear();
                Astronaut.Inventory.Clear();

                Debug.Success("Inventory cleared!");
            }


        }


    }
}
