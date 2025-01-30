

namespace Spacebox.Game.Resources
{

    public class RecipesJSON
    {
        public RecipejSONType[] RecipeCatalog { get; set; } 
    }

    public class RecipejSONType
    {
        public string Type { get; set; } = "unknown";
        public RecipeData[] Recipes { get; set; }
    }
    public class RecipeData
    {
        public string Type { get; set; } = "unknown";
        public int RequiredTicks { get; set; } = 20;
        public int PowerPerTickRequared { get; set; } = 0;
        public ingredient Ingredient { get; set; }
        public product Product { get; set; }
        public struct ingredient
        {
            public string Item { get; set; }
            public int Quantity { get; set; }
        }

        public struct product
        {
            public string Item { get; set; }
            public int Quantity { get; set; }
        }
    }
}
