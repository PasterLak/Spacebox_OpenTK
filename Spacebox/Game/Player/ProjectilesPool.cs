
using Engine;


namespace Spacebox.Game.Player
{

    /* public interface IPoolController <T> where T : IPoolable<T>
     {
         IPoolable<T> Take();
         void PutBack(T item);
     }*/
    public class ProjectilesPool : IDisposable
    {

        private List<Projectile> Projectiles { get; set; }
        private Pool<Projectile> pool;

        public ProjectilesPool(int initCount)
        {
            pool = new Pool<Projectile>(initCount, true);
            Projectiles = new List<Projectile>();
        }

        public Projectile Take()
        {
            var e = pool.GetFreeElement();
            e.OnDespawn += PutBack;
            Projectiles.Add(e);
            return e;
        }

        public void PutBack(Projectile projectile)
        {
            if (projectile != null)
            {
                Projectiles.Remove(projectile);
                projectile.OnDespawn -= PutBack;
                pool.ReturnElement(projectile);
            }
        }

        public void Update()
        {
            for (int i = 0; i < Projectiles.Count; i++)
            {
                Projectiles[i].Update();
            }
        }

        public void Render()
        {
            for (int i = 0; i < Projectiles.Count; i++)
            {
                Projectiles[i].Render();
            }
        }

        public void Dispose()
        {
            var p = Projectiles.ToArray();
           foreach (var e in p)
            {
                e.IsActive = false;
                PutBack(e);
            }
        }

        

    }
}
