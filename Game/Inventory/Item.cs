

using OpenTK.Mathematics;

namespace Spacebox.Game
{
    public class Item
    {
        public short Id;
        public byte StackSize;
        public string Name;
        public string Discription;
        public float ModelDepth = 0.02f;
        public Vector2i TextureCoord = new Vector2i(0,0);
        public IntPtr IconTextureId { get; set; }

        public Item(byte stackSize, string name)
        {
           
            StackSize = stackSize;
            Name = name;
        }

        public Item(short id, byte stackSize, string name)
        {
            Id = id;
            StackSize = stackSize;
            Name = name;
        }

        public Item(short id, byte stackSize, string name, Vector2i texture)
        {
            Id = id;
            StackSize = stackSize;
            Name = name;
            TextureCoord = texture;
        }

        public Item(byte stackSize, string name, byte x, byte y)
        {
           
            StackSize = stackSize;
            Name = name;
            TextureCoord = new Vector2i(x,y);
        }

        public Item(byte stackSize, string name, byte x, byte y, float modelDepth)
        {

            StackSize = stackSize;
            Name = name;
            TextureCoord = new Vector2i(x, y);
            ModelDepth = modelDepth;
        }

        public Item Copy()
        {
            Item item = new Item(Id, StackSize, Name);
            item.TextureCoord = TextureCoord;
            item.IconTextureId = IconTextureId;

            return item;
        }
    }
}
