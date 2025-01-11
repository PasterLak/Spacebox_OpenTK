﻿using Spacebox.Game.GUI;
using Spacebox.Game.Player;
using Spacebox.Game.Resources;

namespace Spacebox.Game.Generation
{
    public class RadarBlock : InteractiveBlock
    {
        public RadarBlock(BlockData blockData) : base(blockData)
        {

            if (RadarWindow.Instance != null)
                OnUse += RadarWindow.Instance.Toggle;
        }

        public override void Use(Astronaut player)
        {
          
            base.Use(player);
        }
    }
}
