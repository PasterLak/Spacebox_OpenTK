using Engine;
using Engine.Components;


namespace Spacebox.Game.Player
{
    public class SpheresPool : Component
    {
        public static SpheresPool Instance;
        private List<ImpulseSphere> spheres;
        private Pool<ImpulseSphere> pool;


        public SpheresPool()
        {
            Instance = this;
           
           
        }

        public override void OnAttached(Node3D onOwner)
        {
            base.OnAttached(onOwner);

            pool = new Pool<ImpulseSphere>(3,
                   obj => obj,
                obj => { },
                obj => { obj.Reset(); },
                obj => obj.IsActive,
                (obj, active) => obj.IsActive = active);

            spheres = new List<ImpulseSphere>();

        }

        public ImpulseSphere Take()
        {
            var sphere = pool.Take();
            spheres.Add(sphere);
            return sphere;
        }

        public void PutBack(ImpulseSphere sphere)
        {
            if (sphere != null)
            {
                spheres.Remove(sphere);
                pool.Release(sphere);
            }
        }


        public override void OnUpdate()
        {
            for (int i = spheres.Count - 1; i >= 0; i--)
            {
                spheres[i].Update();
                if (!spheres[i].IsActive)
                {
                    PutBack(spheres[i]);
                }
            }
        }

        public override void OnRender()
        {
           
            for (int i = 0; i < spheres.Count; i++)
                spheres[i].Render();
        }

        public override void OnDetached()
        {
            foreach (var s in spheres.ToArray())
                PutBack(s);
            Instance = null;


        }
    }
}
