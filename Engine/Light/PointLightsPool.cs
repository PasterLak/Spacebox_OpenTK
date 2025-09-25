namespace Engine.Light
{
    public class PointLightsPool
    {

        private Pool<PointLight> pool;

        public PointLightsPool( int initSize)
        {

            pool = new Pool<PointLight>(initSize,
            obj => obj,
            obj => { obj.Enabled = true; },
            obj => { obj.Enabled = false; },
            obj => obj.Enabled,
            (obj, active) => obj.Enabled = active);

        }


        public PointLight Take()
        {

            return pool.Take();

        }

        public void PutBack(PointLight light)
        {
            if (light == null)
            {
                Debug.Error("[PointLightsPool] PutBack: light was null");
                return;
            }
           
            pool.Release(light);
        }


    }
}
