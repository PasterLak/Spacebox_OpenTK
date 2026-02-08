namespace Engine.Light
{
    public class LightPool<L> where L : LightBase, new()
    {

        private Pool<L> pool;

        public LightPool(int initSize)
        {

            pool = new Pool<L>(initSize,
            obj => obj,
            obj => { obj.Enabled = true; },
            obj => { obj.Enabled = false; },
            (obj, active) => obj.Enabled = active);

        }

        public L Take()
        {
            return pool.Take();
        }

        public void PutBack(L light)
        {
            if (light == null)
            {
                Debug.Error("[LightPool] PutBack: the light object was null");
                return;
            }

            pool.Release(light);
        }


    }
}
