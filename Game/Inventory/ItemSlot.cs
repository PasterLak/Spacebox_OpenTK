

namespace Spacebox.Game
{
    public class ItemSlot
    {
        public Item? Item;
        public byte Count;

        
        public bool IsEmpty => Item == null;



        public void DeleteItem()
        {
            if (Item != null)
            {
                Item = null;
            }
        }
        

    }
}
