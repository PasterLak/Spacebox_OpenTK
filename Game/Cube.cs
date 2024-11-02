using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Spacebox.Common;
using System;
using System.Collections.Generic;

namespace Spacebox.Game
{
    /// <summary>
    /// Enum to specify the type of cube rendering.
    /// </summary>
    public enum CubeType
    {
        Wireframe = 0,
        Textured = 1
    }

    /// <summary>
    /// Represents a cube with its properties.
    /// </summary>
    public class Cube
    {
        public Vector3 Position { get; private set; }
        public CubeType Type { get; private set; }
        public Color4 Color { get; private set; }
        public Vector2 TextureUV { get; private set; } // UV coordinates in the texture atlas

        public Cube(Vector3 position, CubeType type, Color4 color, Vector2 textureUV = default)
        {
            Position = position;
            Type = type;
            Color = color;
            TextureUV = textureUV;
        }
    }
}
