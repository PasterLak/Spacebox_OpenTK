﻿using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.GUI;

namespace Spacebox.Game
{
    public class InteractiveBlock : Block
    {

        public Keys KeyToUse { get; private set; } = Keys.F;
        public string HoverText = "Hover Text";

        public Action OnUse;

        public void Use()
        {
            OnUse?.Invoke();
        }

        public InteractiveBlock(Vector2 textureCoords, Vector3? color = null, float lightLevel = 0, Vector3? lightColor = null) : base(textureCoords, color, lightLevel, lightColor)
        {
        }
        public InteractiveBlock(BlockData blockData) : base(blockData)

        {
            OnUse += RadarWindow.Instance.Toggle;
        }

        
    }
}
