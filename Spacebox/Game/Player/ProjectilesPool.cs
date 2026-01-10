
using Engine;
using Engine.Components;


namespace Spacebox.Game.Player
{

    public class ProjectilesPool : Component
    {

        private List<Projectile> Projectiles { get; set; }
        private Pool<Projectile> pool;

        public ProjectilesPool(int initCount)
        {
            pool = new Pool<Projectile>(initCount,
                 obj => obj,
                 obj => { obj.OnDespawn += PutBack; },
                 obj => { obj.OnDespawn -= PutBack; obj.Reset(); },
                 obj => obj.Enabled,
                 (obj, active) => obj.Enabled = active);

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

        public override void OnUpdate()
        {
            base.OnUpdate();

            for (int i = Projectiles.Count - 1; i >= 0; i--)
                Projectiles[i].Update();
        }

        public override void OnRender()
        {
            base.OnRender();

            for (int i = 0; i < Projectiles.Count; i++)
            {
                Projectiles[i].Render();
            }
        }

        public override void OnDetached()
        {
            base.OnDetached();

            var p = Projectiles.ToArray();
            foreach (var e in p)
            {
                e.Enabled = false;
                PutBack(e);
            }
        }



    }
}
