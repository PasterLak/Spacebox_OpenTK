
using Engine;


namespace Spacebox.Game.Player
{

    public class ProjectilesPool : IDisposable
    {

        private List<Projectile> Projectiles { get; set; }
        private Pool<Projectile> pool;

        public ProjectilesPool(int initCount)
        {
            pool = new Pool<Projectile>(initCount,
                 obj => obj,
                 obj => { obj.OnDespawn += PutBack; },
                 obj => { obj.OnDespawn -= PutBack; obj.Reset(); },
                 obj => obj.IsActive,
                 (obj, active) => obj.IsActive = active);

            Projectiles = new List<Projectile>();
        }

        public Projectile Take()
        {
            var e = pool.Take();
          
            Projectiles.Add(e);
            return e;
        }

        public void PutBack(Projectile projectile)
        {
            if (projectile != null)
            {
                Projectiles.Remove(projectile);
                
                pool.Release(projectile);
            }
        }

        public void Update()
        {
            for (int i = Projectiles.Count - 1; i >= 0; i--)
                Projectiles[i].Update();
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
