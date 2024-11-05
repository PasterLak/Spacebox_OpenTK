using OpenTK.Mathematics;
using Spacebox.Game;

namespace Spacebox.Extensions
{
    public static class FaceExtensions
    {
        public static Vector3i GetNormal(this Face face)
        {
            return face switch
            {
                Face.Left => new Vector3i(-1, 0, 0),
                Face.Right => new Vector3i(1, 0, 0),
                Face.Bottom => new Vector3i(0, -1, 0),
                Face.Top => new Vector3i(0, 1, 0),
                Face.Back => new Vector3i(0, 0, -1),
                Face.Front => new Vector3i(0, 0, 1),
                _ => Vector3i.Zero,
            };
        }
    }
}
