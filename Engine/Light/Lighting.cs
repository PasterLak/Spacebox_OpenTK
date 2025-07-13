
using OpenTK.Mathematics;
using System.Drawing;


namespace Engine.Light
{
    public static class Lighting
    {
        public static Vector3 AmbientColor = new Vector3(1);
        public static Vector3 FogColor;

        private static float _fogDensity = 32 / 10_000.0f;
        public static float FogDensity
        {
            get => _fogDensity;
            set { _fogDensity = value / 10_000.0f; }
        }

        private static Node3D _skybox;
        public static Node3D? Skybox
        {
            get => _skybox;
            set
            {
                if (_skybox != null)
                {
                    _skybox.Destroy();
                }
                _skybox = value;
            }
        }

        public static void AddAmbient()
        {
            AmbientColor += new Vector3(0.1f, 0.1f, 0.1f);

            if (AmbientColor.X > 1)
                AmbientColor = new Vector3
                    (1, AmbientColor.Y, AmbientColor.Z);

            if (AmbientColor.Y > 1)
                AmbientColor = new Vector3
                    (AmbientColor.X, 1, AmbientColor.Z);

            if (AmbientColor.Z > 1)
                AmbientColor = new Vector3
                    (AmbientColor.X, AmbientColor.Y, 1);

            //AmbientSaveLoadManager.SaveAmbient();
        }

        public static void RemoveAmbient()
        {
            AmbientColor -= new Vector3(0.1f, 0.1f, 0.1f);

            if (AmbientColor.X < 0)
                AmbientColor = new Vector3
                    (0, AmbientColor.Y, AmbientColor.Z);

            if (AmbientColor.Y < 0)
                AmbientColor = new Vector3
                    (AmbientColor.X, 0, AmbientColor.Z);

            if (AmbientColor.Z < 0)
                AmbientColor = new Vector3
                    (AmbientColor.X, AmbientColor.Y, 0);

            //AmbientSaveLoadManager.SaveAmbient();
        }
    }
}
