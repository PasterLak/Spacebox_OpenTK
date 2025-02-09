using Engine;


namespace Spacebox.Game.Player
{
    public class SpheresPool : IDisposable
    {
        public static SpheresPool Instance;
        private List<ImpulseSphere> spheres;
        private List<ImpulseSphere> spheresToPutBack;
        private Pool<ImpulseSphere> pool;


        public SpheresPool()
        {
            Instance = this;
                 pool = new Pool<ImpulseSphere>(3, true);
            spheres = new List<ImpulseSphere>();
            spheresToPutBack = new List<ImpulseSphere>();
        }

        public ImpulseSphere Take()
        {
            var sphere = pool.GetFreeElement();
            spheres.Add(sphere);
            return sphere;
        }

        public void PutBack(ImpulseSphere sphere)
        {
            if (sphere != null)
            {
                spheres.Remove(sphere);
                pool.ReturnElement(sphere);
            }
        }

      
        public void Update()
        {
            if(spheresToPutBack.Count > 0)
            {
                foreach(var sphere in spheresToPutBack)
                {
                    PutBack(sphere);
                }

                spheresToPutBack.Clear();
            }

            for (int i = 0; i < spheres.Count; i++)
            {
                spheres[i].Update(Time.Delta);
                
                if(!spheres[i].IsActive)
                {
                    spheresToPutBack.Add(spheres[i]);
                }
            }
               
        }

        public void Render()
        {
            for (int i = 0; i < spheres.Count; i++)
                spheres[i].Render();
        }

        public void Dispose()
        {
            foreach (var s in spheres.ToArray())
                PutBack(s);

            
        }
    }
}
