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
        public Product[] Products;
     
        public Blueprint() 
        {
        }
    }

}
