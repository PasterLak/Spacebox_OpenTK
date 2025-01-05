﻿using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.GUI;
using Spacebox.Game.Player;
using Spacebox.Game.Resources;

namespace Spacebox.Game.Generation
{
    public class InteractiveBlock : Block
    {

        public Keys KeyToUse { get; private set; } = Keys.F;
        public string HoverText = "Hover Text";

        public Action<Astronaut> OnUse;

        public virtual void Use(Astronaut player)
        {
            OnUse?.Invoke(player);
        }

        public InteractiveBlock(Vector2 textureCoords, Vector3? color = null, float lightLevel = 0, Vector3? lightColor = null) 
            : base(textureCoords, color, lightLevel, lightColor)
        {
        }
        public InteractiveBlock(BlockData blockData) : base(blockData)

        {
            if(blockData.Name == "Radar")
            {
                OnUse += RadarWindow.Instance.Toggle;
            }
           
            
        }


    }
}
