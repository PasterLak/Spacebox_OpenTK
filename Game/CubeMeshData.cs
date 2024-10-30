using OpenTK.Mathematics;

namespace Spacebox.Game
{
    public enum Face
    {
        Left,
        Right,
        Bottom,
        Top,
        Back,
        Front
    }

    public static class CubeMeshData
    {
        public static Vector3[] GetFaceVertices(Face face)
        {
            switch (face)
            {
                case Face.Front:
                    return new Vector3[]
                    {
                        new Vector3(0, 0, 1),
                        new Vector3(1, 0, 1),
                        new Vector3(1, 1, 1),
                        new Vector3(0, 1, 1)
                    };
                case Face.Back:
                    return new Vector3[]
                    {
                        new Vector3(1, 0, 0),
                        new Vector3(0, 0, 0),
                        new Vector3(0, 1, 0),
                        new Vector3(1, 1, 0)
                    };
                case Face.Left:
                    return new Vector3[]
                    {
                        new Vector3(0, 0, 0),
                        new Vector3(0, 0, 1),
                        new Vector3(0, 1, 1),
                        new Vector3(0, 1, 0)
                    };
                case Face.Right:
                    return new Vector3[]
                    {
                        new Vector3(1, 0, 1),
                        new Vector3(1, 0, 0),
                        new Vector3(1, 1, 0),
                        new Vector3(1, 1, 1)
                    };
                case Face.Top:
                    return new Vector3[]
                    {
                        new Vector3(0, 1, 1),
                        new Vector3(1, 1, 1),
                        new Vector3(1, 1, 0),
                        new Vector3(0, 1, 0)
                    };
                case Face.Bottom:
                    return new Vector3[]
                    {
                        new Vector3(0, 0, 0),
                        new Vector3(1, 0, 0),
                        new Vector3(1, 0, 1),
                        new Vector3(0, 0, 1)
                    };
                default:
                    return null;
            }
        }
    }
}
