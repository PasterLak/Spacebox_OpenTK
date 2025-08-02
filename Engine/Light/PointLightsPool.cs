namespace Engine.Light
{
    public class PointLightsPool : IDisposable
    {

        public static PointLightsPool? Instance;

        private Pool<PointLight> pool;
        private List<PointLight> activeLights;
    


        public PointLightsPool( int initSize)
        {
            if (Instance == null)
                Instance = this;
            else
            {
                Instance = this;
                Debug.Error("[PointLightsPool] Instance already exists!");
            }

         
            pool = new Pool<PointLight>(initSize, true);
            activeLights = new List<PointLight>();

            foreach (PointLight light in pool.GetAllObjects())
            {
               // light.Shader = shader;
            }
        }


        public PointLight Take()
        {

            var light = pool.Take();
            activeLights.Add(light);
            light.Enabled = true;

            return light;
        }

        public void PutBack(PointLight light)
        {
            if (light == null)
            {
                Debug.Error("[PointLightsPool] PutBack: light was null");
                return;
            }
            light.Enabled = false;
            activeLights.Remove(light);
            pool.Release(light);
        }


        public void Render()
        {
        
            int count = activeLights.Count;

            if (count == 0)
            {
             
                return;
            }

            var active = 0;
            bool[] ind = new bool[count];
            for (int i = 0; i < count; i++)
            {
                if (activeLights[i].Enabled)
                {
                    ind[i] = true;
                    active++;
                    continue;
                }
                ind[i] = false;
            }

            if (active == 0)
            {
               // shader.SetInt("pointLightCount", 0);
                return;
            }

           // shader.SetInt("pointLightCount", active);
            active = 0;
            for (int i = 0; i < count; i++)
            {

                if (ind[i])
                {
                    var light = activeLights[i];
                   // light.SetShaderParams(active++, Camera.Main);
                }

            }
        }
        public void Dispose()
        {
            Instance = null;
        }


    }
}
