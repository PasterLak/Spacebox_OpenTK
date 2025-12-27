using OpenTK.Mathematics;
using Engine;

namespace Spacebox.Game.Resource;

public partial class AtlasTexture
{
    private class AtlasTextureData
    {
        public string Name { get; set; }
        public Vector2Byte Size { get; set; }
        public Vector2Byte Position { get; set; }
        public Vector2[] UV { get; set; }
        public Texture2D Texture { get; set; }

        public override string ToString()
        {
            return $"Name:{Name}, Size:{Size}, Pos:{Position}, UV:{UV[0]} to {UV[2]}";
        }
    }
}
