

namespace Spacebox.Game
{

    public class Ingredient
    {
        public Item Item;
        public byte Quantity;
    }
    public class Product
    {
        public Item Item;
        public byte Quantity;
    }
    public class Blueprint
    {

        public short Id;

        public Ingredient[] Ingredients;
        public Product[] Products;
     
        public Blueprint() 
        {
        }
    }

}
