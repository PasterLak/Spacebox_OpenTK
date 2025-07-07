using OpenTK.Mathematics;
using System.Drawing;


namespace Engine
{
    public static class Lighting
    {
        public static Vector3 AmbientColor = new Vector3(1);
        public static Color BackgroundColor = Color.Black;
        public static Vector3 FogColor;
        public static float FogDensity = 0.1f;

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
