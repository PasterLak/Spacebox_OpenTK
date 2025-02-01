
using System.Numerics;
using Engine;
namespace Spacebox.GUI
{
    public class GUIHelper
    {
        public const int VirtualScreenX = 1920;
        public static Vector2 CalculateSize(Vector2 virtualSize, OpenTK.Mathematics.Vector2 sizeNow)
        {
            if (virtualSize == Vector2.Zero) return Vector2.Zero;

            var xyRatio = virtualSize.X / virtualSize.Y;
            var yDelta = VirtualScreenX / virtualSize.Y;
            var yNew = sizeNow.Y / yDelta;


            return new Vector2(yNew * xyRatio, yNew);
        }

        public static Vector2 CalculateSize(Vector2 virtualSize, Vector2 sizeNow)
        {
            var xyRatio = virtualSize.X / virtualSize.Y;
            var yDelta = VirtualScreenX / virtualSize.Y;
            var yNew = sizeNow.Y / yDelta;


            return new Vector2(yNew * xyRatio, yNew);
        }

       
    }
}
