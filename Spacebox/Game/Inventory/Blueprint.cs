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
            var name = Item.Name;

            var prefix = (name != "$health" ? "x" : "");

            if (name == "$health") name = "Health";
            return prefix + Quantity + " " + name;
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

        public Blueprint Clone()
        {
            Blueprint copy = new Blueprint();

            copy.Id = this.Id;
            copy.RequiredTicks = this.RequiredTicks;
            copy.PowerPerTickRequared = this.PowerPerTickRequared;

            if (this.Product != null)
            {
                copy.Product = new Product(this.Product.Item, this.Product.Quantity);
            }

            if (this.Ingredients != null)
            {

                copy.Ingredients = new Ingredient[this.Ingredients.Length];

                for (int i = 0; i < this.Ingredients.Length; i++)
                {
                    var original = this.Ingredients[i];
                    if (original != null)
                    {
                        copy.Ingredients[i] = new Ingredient(original.Item, original.Quantity);
                    }
                }
            }

            return copy;
        }

        public static void ScaleBlueprint(Blueprint blueprint, int multiplier)
        {
            if (blueprint == null) return;

            if (blueprint.Product != null)
            {
                blueprint.Product.Quantity = (byte)(blueprint.Product.Quantity * multiplier);
            }

            if (blueprint.Ingredients != null)
            {
                foreach (var ingredient in blueprint.Ingredients)
                {
                    if (ingredient != null)
                    {
                        ingredient.Quantity = (byte)(ingredient.Quantity * multiplier);
                    }
                }
            }
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
