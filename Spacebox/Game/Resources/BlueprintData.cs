namespace Spacebox.Game.Resources
{

    public class BlueprintData
    {
        public ingridient[] Ingredients { get; set; }
        public product Product { get; set; }


        public class ingridient
        {
            public string Item { get; set; } = "";
            public int Quantity { get; set; } = 0;
        }
        public class product
        {
            public string Item { get; set; } = "";
            public int Quantity { get; set; } = 0;
        }
    }
}
