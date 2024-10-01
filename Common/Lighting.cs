using OpenTK.Mathematics;
using System.Drawing;


namespace Spacebox.Common
{
    public static class Lighting
    {
        public static Vector3 AmbientColor = new Vector3(0.4f, 0.35f, 0.4f);
        public static Color BackgroundColor = Color.Black;

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
        }
    }
}
