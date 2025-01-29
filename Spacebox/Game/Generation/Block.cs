using OpenTK.Mathematics;
using Spacebox.Game.Resources;
using System.Runtime.CompilerServices;
using Spacebox.Common;

namespace Spacebox.Game.Generation
{
    public enum Direction : byte
    {
        Up = 0,
        Down = 1,
        Left = 2,
        Right = 3,
        Back = 4,
        Forward = 5
    }

    // bit packing: id= 16, dir=3, mass=8, health=8, trans=1 light=1 emission=1 air=1
    public class Block
    {
        
       

        public short BlockId { get; set; } = 0;
        public byte Mass { get; private set; } = 1;
        public byte Durability { get; set; } = 1;

        public Vector3 Color { get; set; }
        public bool IsTransparent { get; set; } = false;

        public Direction Direction = Direction.Up;

        // local data
        public float LightLevel { get; set; } = 0; //0 - 15
        public Vector3 LightColor { get; set; } = Vector3.Zero;
        public bool enableEmission = true;
        

        public Block()
        {
            Color =  new Vector3(1.0f, 1.0f, 1.0f);
            LightLevel = 0f;
            LightColor = Vector3.Zero;
        }

        public Block(BlockData blockData)
        {
            BlockId = blockData.Id;

            IsTransparent = blockData.IsTransparent;

            Color = new Vector3(1.0f, 1.0f, 1.0f);
            LightLevel = 0;
            Mass = blockData.Mass;
            Durability = blockData.Health;
            LightColor = blockData.LightColor;

            if (LightColor != Vector3.Zero)
            {
                LightLevel = 15;
            }
        }
        public bool Is<T>() where T : Block
        {
            return this is T;
        }
        public bool Is<T>(out T res) where T : Block
        {
            res = default;

            if (this is T)
            {
                res = this as T;
                return true;
            }

            return false;
        }

        public bool IsAir()
        {
            return BlockId == 0;
        }

        public bool IsLight()
        {
            return LightLevel > 0;
        }

        public override string ToString()
        {
            var c = RoundVector3(Color);
            var ll = RoundFloat(LightLevel);
            var lc = RoundVector3(LightColor);

            return $"Id: {BlockId} ({GameBlocks.Block[BlockId].Name})\n" +
                   $"C: {c}, LL: {ll}, LC: {lc}\n" +
                   $"Direction: {Direction}" +
                   $"\nTransparent: {IsTransparent}";
        }


        // -----------------------------------------------------------------------------------

        private static float RoundFloat(float x)
        {
            return (float)Math.Round(x, MidpointRounding.ToEven);
        }

        public static Vector3 RoundVector3(Vector3 v)
        {
            return new Vector3(RoundFloat(v.X), RoundFloat(v.Y), RoundFloat(v.Z));
        }

        public void SetDirectionFromNormal(Vector3 normal)
        {
            Direction = GetDirectionFromNormal(normal);
        }
        public void SetDirectionFromNormal(Vector3SByte normal)
        {
            Direction = GetDirectionFromNormal(normal);
        }

        private const float epsilon = 1e-6f;

        public static Direction GetDirectionFromNormalOld(Vector3 normal)
        {
            if (Math.Abs(normal.X - 1f) < epsilon) return Direction.Right;
            if (Math.Abs(normal.X + 1f) < epsilon) return Direction.Left;
            if (Math.Abs(normal.Y - 1f) < epsilon) return Direction.Up;
            if (Math.Abs(normal.Y + 1f) < epsilon) return Direction.Down;
            if (Math.Abs(normal.Z - 1f) < epsilon) return Direction.Forward;
            if (Math.Abs(normal.Z + 1f) < epsilon) return Direction.Back;

            return Direction.Up;
        }

        const float a1 = 1f - epsilon;
        const float a2 = -1f + epsilon;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Direction GetDirectionFromNormal(Vector3SByte normal)
        {
            return GetDirectionFromNormal(new Vector3(normal.X, normal.Y, normal.Z));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Direction GetDirectionFromNormal(Vector3 normal)
        {
            if (normal.X > a1)
                return Direction.Right;
            if (normal.X < a2)
                return Direction.Left;
            if (normal.Y > a1)
                return Direction.Up;
            if (normal.Y < a2)
                return Direction.Down;
            if (normal.Z > a1)
                return Direction.Forward;
            if (normal.Z < a2)
                return Direction.Back;

            return Direction.Up;
        }

        public Vector3 GetVectorFromDirection()
        {
            return GetVectorFromDirection(Direction);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 GetVectorFromDirection(Direction direction)
        {
            switch (direction)
            {
                case Direction.Up:
                    return Vector3.UnitY;
                case Direction.Down:
                    return -Vector3.UnitY;
                case Direction.Left:
                    return -Vector3.UnitX;
                case Direction.Right:
                    return Vector3.UnitX;
                case Direction.Forward:
                    return Vector3.UnitZ;
                case Direction.Back:
                    return -Vector3.UnitZ;
                default:
                    return Vector3.Zero;
            }
        }


    }
}