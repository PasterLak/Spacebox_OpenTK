using OpenTK.Mathematics;

using Spacebox.Game.GUI;
using Spacebox.Game.Player;
using Engine;
using Engine.Commands;

namespace Spacebox.Game.Commands
{
    internal class TagCommand : CommandBase
    {
        public override string Name => "tag";

        public override string Description => "Tag <create/delete> <name>";

        public Astronaut Astronaut { get; set; }


        public TagCommand(Astronaut astronaut)
        {
            this.Astronaut = astronaut;
        }
        public override void Execute(string[] args)
        {

            if (ValidateArgs(args, 2) == false) return;

            if (Astronaut == null)
            {
                Debug.Error("Astronaut reference is null.");
                return;
            }


            if (args[0] == "delete")
            {

                TagManager.Instance.ReleaseTagByText(args[1]);
                Debug.Success("Tag deleted!: " + args[1]);

            }

            if (args[0] == "visible")
            {


                if (TagManager.Instance.TryGetTagByName(args[1], out var tag))
                {
                    tag.Visible = !tag.Visible;
                    Debug.Success("Tag visibility changed!: " + args[1] + " to " + tag.Visible);
                }
                else
                {
                    Debug.Error("Tag not found!: " + args[1]);
                }

            }

            else if (args[0] == "create")
            {

                TagManager.Instance.CreateTag(args[1], Astronaut.Position, Color4.Yellow, true);
                Debug.Success("Tag added!: " + args[1]);


                if (args.Length == 5)
                {
                    var r = int.Parse(args[1]);
                    var g = int.Parse(args[2]);
                    var b = int.Parse(args[3]);


                    TagManager.Instance.CreateTag(args[1], Astronaut.Position, new Color4(r, g, b, 1), true);

                    Debug.Success("Tag added!: " + args[1]);
                }
            }
            else
            {

            }


        }


    }
}
