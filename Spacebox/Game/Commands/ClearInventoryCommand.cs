using OpenTK.Mathematics;
using Spacebox.Common;
using Spacebox.Common.Commands;
using Spacebox.Game.Player;

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
                Debug.AddMessage("Astronaut reference is null.", Color4.Red);
                return;
            }

            if (args.Length == 0)
            {
                Astronaut.Panel.Clear();
                Astronaut.Inventory.Clear();

                Debug.AddMessage("Inventory cleared!", Color4.Green);
            }


        }


    }
}
