namespace Spacebox.Game.Resource
{

    public class BlueprintJSON
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
