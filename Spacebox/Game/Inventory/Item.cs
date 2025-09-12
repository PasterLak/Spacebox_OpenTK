using Engine;
using OpenTK.Mathematics;

namespace Spacebox.Game
{
    public class Item
    {
        public short Id;
        public string Id_string;
        public byte StackSize;
        public string Name;
        public string Category { get; set; } = "";
        public string Description { get; set; } = "";
        public float ModelDepth = 1f;
        public Vector2i TextureCoord = new Vector2i(0,0);
        public IntPtr IconTextureId { get; set; }

        public Color3Byte Color { get; set; } = new Color3Byte(0);
        public bool IsLuminous => Color != Color3Byte.Zero;
        public bool IsStackable => StackSize > 1;
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

        public static bool operator ==(Item item1, Item item2)
        {
            if (ReferenceEquals(item1, item2))
                return true;

            if (item1 is null || item2 is null)
                return false;

            return item1.Id == item2.Id;
        }

        public static bool operator !=(Item item1, Item item2)
        {
            return !(item1 == item2);
        }

        public override bool Equals(object obj)
        {
            if (obj is Item other)
                return this == other;
            return false;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
