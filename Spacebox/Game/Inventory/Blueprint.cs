using Engine;
namespace Spacebox.Game
{

    public class Ingredient
    {
        public Item Item;
        public byte Quantity;

        public Ingredient(Item item, byte quantity)
        {
            Item = item;
            Quantity = quantity;
        }

        public override string ToString()
        {
            if(Item == null) return string.Empty;
            return "x"+ Quantity + " " + Item.Name;
        }
    }
    public class Product
    {
        public Item Item;
        public byte Quantity;

        public Product(Item item, byte quantity)
        {
            Item = item;
            Quantity = quantity;
        }

        public override string ToString()
        {
            if (Item == null) return string.Empty;
            return "x" + Quantity + " " + Item.Name;
        }
    }
    public class RecipeBase
    {
        public short Id;
        public short RequiredTicks;
        public short PowerPerTickRequared;
    }
    public class Recipe : RecipeBase
    {
        public Ingredient Ingredient;
        public Product Product;

        
    }
    public class Blueprint : RecipeBase
    {

        public Ingredient[] Ingredients;
        public Product Product;
     
        public Blueprint() 
        {
        }

        public override string ToString()
        {
            string s = "";

            if(Product != null && Product.Item != null) 
            {
                s = Product.Item.Name + "\n";
             }

            foreach(var p in Ingredients)
            {
                s += "x" + p.Quantity + " " + p.Item.Name + "\n";
            }

            return s;
        }
    }

}
