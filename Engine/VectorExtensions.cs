using OpenTK.Mathematics;


namespace Engine
{
    public static class VectorExtensions
    {
        public static System.Numerics.Vector2 ToSystemVector2(this OpenTK.Mathematics.Vector2 vec)
        {
            return new System.Numerics.Vector2(vec.X, vec.Y);
        }

        public static OpenTK.Mathematics.Vector2 ToOpenTKVector2(this System.Numerics.Vector2 vec)
        {
            return new OpenTK.Mathematics.Vector2(vec.X, vec.Y);
        }

        public static System.Numerics.Vector3 ToSystemVector3(this OpenTK.Mathematics.Vector3 vec)
        {
            return new System.Numerics.Vector3(vec.X, vec.Y, vec.Z);
        }

        public static OpenTK.Mathematics.Vector3 ToOpenTKVector3(this System.Numerics.Vector3 vec)
        {
            return new OpenTK.Mathematics.Vector3(vec.X, vec.Y, vec.Z);
        }

        public static System.Numerics.Vector4 ToSystemVector4(this OpenTK.Mathematics.Vector4 vec)
        {
            return new System.Numerics.Vector4(vec.X, vec.Y, vec.Z, vec.W);
        }

        public static OpenTK.Mathematics.Vector4 ToOpenTKVector4(this System.Numerics.Vector4 vec)
        {
            return new OpenTK.Mathematics.Vector4(vec.X, vec.Y, vec.Z, vec.W);
        }

        public static System.Numerics.Vector4 ToSystemVector4(this Color4 vec)
        {
            return new System.Numerics.Vector4(vec.R, vec.G, vec.B, vec.A);
        }
    }
}
