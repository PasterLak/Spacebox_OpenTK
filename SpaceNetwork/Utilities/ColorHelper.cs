using System.Numerics;

namespace SpaceNetwork.Utilities

{
    public static class ColorHelper
    {

        private static readonly string[] BrightColorHexes = new string[]
        {
            "#FF0000", "#00FF00", "#0000FF", "#FFFF00", "#FF00FF",
            "#00FFFF", "#FFA500", "#FFC0CB", "#FFD700", "#7FFF00",
            "#00FF7F", "#40E0D0", "#1E90FF", "#BA55D3", "#FF1493",
            "#00BFFF", "#32CD32", "#FF4500", "#DA70D6", "#EE82EE"
        };
        private static int lastColorIndex = -1;
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

        public static string GetRandomColorFromListHex(Random rand)
        {
            int index;
            do
            {
                index = rand.Next(BrightColorHexes.Length);
            } while (BrightColorHexes.Length > 1 && index == lastColorIndex);
            lastColorIndex = index;
            return BrightColorHexes[index];
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
