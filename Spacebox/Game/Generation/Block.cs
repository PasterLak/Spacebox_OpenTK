﻿using OpenTK.Mathematics;
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

    // bit packing: id=12 from 16, dir=3, mass=8, health=8, trans=1
    public class Block
    {
        public short BlockId { get; set; } = 0;
        public byte Mass { get; private set; } = 1;
        public byte Health { get; private set; } = 1;

        public Vector3 Color { get; set; }
        public bool IsTransparent { get; set; } = false;

        public Direction Direction = Direction.Up;

        // local data
        public float LightLevel { get; set; } = 0; //0 - 15
        public Vector3 LightColor { get; set; } = Vector3.Zero;

        public const float Diagonal = 1.5f;
        public const float DiagonalSquared = Diagonal * Diagonal;

        public Block()
        {
        }

        public Block(Vector2 textureCoords, Vector3? color = null, float lightLevel = 0f, Vector3? lightColor = null)
        {
            Color = color ?? new Vector3(1.0f, 1.0f, 1.0f);
            LightLevel = lightLevel;
            LightColor = lightColor ?? Vector3.Zero;
        }

        public Block(BlockData blockData)
        {
            BlockId = blockData.Id;

            IsTransparent = blockData.IsTransparent;

            Color = new Vector3(1.0f, 1.0f, 1.0f);
            LightLevel = 0;
            Mass = blockData.Mass;
            Health = blockData.Health;
            LightColor = blockData.LightColor;

            if (LightColor != Vector3.Zero)
            {
                LightLevel = 15;
            }
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
    }
}