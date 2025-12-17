
using Engine.Commands;
using Spacebox.Game.Player;
using Engine;
using Spacebox.Game.Player.GameModes;

namespace Spacebox.Game.Commands
{
    internal class GameModCommand : CommandBase
    {
        public override string Name => "gm";

        public override string Description => "Change gamemode. Usage: gm <0,1,2>  (survival,creative,spectator)";

        public Astronaut Astronaut { get; set; }


        public GameModCommand(Astronaut astronaut)
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

            if (ValidateArgs(args, 1) == false) return;



            if (TryParse<int>(args[0], out var id))
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
                    Debug.Success("Gamemod changed to " + gm.ToString());
                }
                else
                {
                    Debug.Error("Wrong game mode id!");
                }

            }





        }


    }
}