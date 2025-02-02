using System.Numerics;

namespace SpaceNetwork.Utilities

{
    public static class ColorHelper
    {
        public static string VectorToHex(Vector3 color)
        {
            int r = (int)(color.X * 255);
            int g = (int)(color.Y * 255);
            int b = (int)(color.Z * 255);
            return $"#{r:X2}{g:X2}{b:X2}";
        }

        public static Vector3 HexToVector(string hex)
        {
            hex = hex.Replace("#", "");
            byte r = Convert.ToByte(hex.Substring(0, 2), 16);
            byte g = Convert.ToByte(hex.Substring(2, 2), 16);
            byte b = Convert.ToByte(hex.Substring(4, 2), 16);
            return new Vector3(r / 255f, g / 255f, b / 255f);
        }

        private static string GetRandomColorHex(Random rand)
        {
            return $"#{rand.Next(256):X2}{rand.Next(256):X2}{rand.Next(256):X2}";
        }

        public static Vector3 GetRandomColor(Random rand)
        {
            return HexToVector(GetRandomColorHex(rand));
        }

    }
}
