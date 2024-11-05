﻿using OpenTK.Mathematics;
using Spacebox.Common;
using Spacebox.Common.Commands;
using Spacebox.GUI;
using System;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Spacebox.Game.Commands
{
    internal class TagCommand : ICommand
    {
        public string Name => "tag";

        public string Description => "Create or delete tags";

        public Astronaut Astronaut { get; set; }


        public TagCommand(Astronaut astronaut)
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

            if (args[0] == "delete")
            {
                if(args.Length == 2)
                {
                    TagManager.UnregisterTagByText(args[1]);
                }
            }

            else if (args[0] == "create")
            {
                if(args.Length == 2)
                {
                    TagManager.RegisterTag(new Tag(args[1], Astronaut.Position, Color4.Yellow, true));
                }

                if (args.Length == 5)
                {
                    var r = int.Parse(args[1]);
                    var g = int.Parse(args[2]);
                    var b = int.Parse(args[3]);

                    TagManager.RegisterTag(new Tag(args[1], Astronaut.Position, new Color4(r,g,b,1), true));
                }
            }
            else
            {

            }

           
        }

     
    }
}
