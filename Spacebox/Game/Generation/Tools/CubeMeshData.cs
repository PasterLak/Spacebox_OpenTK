using OpenTK.Mathematics;
using Engine;

namespace Spacebox.Game.Generation.Tools
{
    public enum Face : byte
    {
        Left = 0,
        Right = 1,
        Down = 2,
        Up = 3,
        Back = 4,
        Forward = 5
    }

    public static class CubeMeshData
    {
        public static Vector3SByte GetNormal(this Face face)
        {
            return face switch
            {
                Face.Left => new Vector3SByte(-1, 0, 0),
                Face.Right => new Vector3SByte(1, 0, 0),
                Face.Down => new Vector3SByte(0, -1, 0),
                Face.Up => new Vector3SByte(0, 1, 0),
                Face.Back => new Vector3SByte(0, 0, -1),
                Face.Forward => new Vector3SByte(0, 0, 1),
                _ => Vector3SByte.Zero,
            };
        }
        public static Vector2[] GetBasicUVs()
        {
            return new Vector2[]
                    {
                        new Vector2(0, 0),
                        new Vector2(1, 0),
                        new Vector2(1, 1),
                        new Vector2(0, 1)
                    };
        }
        public static Vector3[] GetFaceVertices(Face face)
        {
            switch (face)
            {
                case Face.Forward:
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
                case Face.Up:
                    return new Vector3[]
                    {
                        new Vector3(0, 1, 1),
                        new Vector3(1, 1, 1),
                        new Vector3(1, 1, 0),
                        new Vector3(0, 1, 0)
                    };
                case Face.Down:
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


