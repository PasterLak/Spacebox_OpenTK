using OpenTK.Mathematics;

using Engine.Commands;
using Spacebox.Game.Player;
using Engine;

namespace Spacebox.Game.Commands
{
    internal class GameModCommand : ICommand
    {
        public string Name => "gm";

        public string Description => "gamemod <0,1,2>";

        public Astronaut Astronaut { get; set; }


        public GameModCommand(Astronaut astronaut)
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

            if (args.Length == 1)
            {

                if (int.TryParse(args[0], out int id))
                {
                    GameMode gm = GameMode.Spectator;
                    if (id == 0)
                    {
                        gm = GameMode.Survival;
                    }
                    if (id == 1)
                    {
                        gm = GameMode.Creative;
                    }
                    if (id == 2)
                    {
                        gm = GameMode.Spectator;
                    }

                    if (id >= 0 && id <= 2)
                    {
                        Astronaut.GameMode = gm;
                        Debug.AddMessage("Gamemod changed to " + gm.ToString() , Color4.Green);
                    }
                    else
                    {
                        Debug.Error("Wrong game mode id!");
                    }
                    
                }

                
            }
           

        }


    }
}