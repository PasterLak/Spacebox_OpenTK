using OpenTK.Mathematics;

namespace Spacebox.Game
{
    public class Item
    {
        public short Id;
        public byte StackSize;
        public string Name;
        public string Category { get; set; } = "";
        public string Description;
        public float ModelDepth = 1f;
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

        public Item(byte stackSize, string name, byte x, byte y)
        {
           
            StackSize = stackSize;
            Name = name;
            TextureCoord = new Vector2i(x,y);
        }

        public Item(byte stackSize, string name, float modelDepth)
        {

            StackSize = stackSize;
            Name = name;
          
            ModelDepth = modelDepth;
        }

        public bool Is<T>() where T : Item
        {
            return this is T;
        }

        public bool Is<T>(out T res) where T : Item
        {
            res = default;

            if (this is T)
            {
                res = this as T;
                return true;
            }

            return false;
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
