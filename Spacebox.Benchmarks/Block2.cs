using OpenTK.Mathematics;
using Spacebox.Game.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Spacebox.Game.Generation
{
    public enum Direction2 : byte
    {
        Up = 0,
        Down = 1,
        Left = 2,
        Right = 3,
        Back = 4,
        Forward = 5
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct Block2
    {
        private int _data;
     
        // Bit masks and shifts
        private const int BlockIdBits = 12;
        private const int DirectionBits = 3;
        private const int MassBits = 8;
        private const int HealthBits = 8;
        private const int TransparencyBits = 1;
       

        private const int BlockIdShift = 0;
        private const int DirectionShift = BlockIdBits;
        private const int MassShift = DirectionShift + DirectionBits;
        private const int HealthShift = MassShift + MassBits;
        private const int TransparencyShift = HealthShift + HealthBits;

        private const int BlockIdMask = (1 << BlockIdBits) - 1;
        private const int DirectionMask = (1 << DirectionBits) - 1;
        private const int MassMask = (1 << MassBits) - 1;
        private const int HealthMask = (1 << HealthBits) - 1;
        private const int TransparencyMask = (1 << TransparencyBits) - 1;

        // Properties for accessing packed data
        public int BlockId
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_data >> BlockIdShift) & BlockIdMask;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _data = (_data & ~(BlockIdMask << BlockIdShift)) | ((value & BlockIdMask) << BlockIdShift);
        }

        public Direction2 Direction
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (Direction2)((_data >> DirectionShift) & DirectionMask);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _data = (_data & ~(DirectionMask << DirectionShift)) | (((int)value & DirectionMask) << DirectionShift);
        }
        
        public int Mass
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_data >> MassShift) & MassMask;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _data = (_data & ~(MassMask << MassShift)) | ((value & MassMask) << MassShift);
        }

        public int Health
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_data >> HealthShift) & HealthMask;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _data = (_data & ~(HealthMask << HealthShift)) | ((value & HealthMask) << HealthShift);
        }

        public bool IsTransparent
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ((_data >> TransparencyShift) & TransparencyMask) != 0;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _data = (_data & ~(TransparencyMask << TransparencyShift)) | ((value ? 1 : 0) << TransparencyShift);
        }

        public Vector3 Color { get; set; }
        public float LightLevel { get; set; } = 0;
        public Vector3 LightColor { get; set; } = Vector3.Zero;

        public Block2(int blockId = 0, Direction2 direction = Direction2.Up, int mass = 0, int health = 0, bool isTransparent = false)
        {
            BlockId = blockId;
            Direction = direction;
            Mass = mass;
            Health = health;
            IsTransparent = isTransparent;
            Color = new Vector3(1.0f, 1.0f, 1.0f);
        }

        public bool IsAir => BlockId == 0;

        public bool IsLight => LightLevel > 0;

        public override string ToString()
        {
            return $"Id: {BlockId}, Direction: {Direction}, Mass: {Mass}, Health: {Health}, Transparent: {IsTransparent}";
        }

        public void SetDirectionFromNormal(Vector3 normal)
        {
            Direction = GetDirectionFromNormal(normal);
        }

        private const float epsilon = 1e-6f;
        private const float a1 = 1f - epsilon;
        private const float a2 = -1f + epsilon;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Direction2 GetDirectionFromNormal(Vector3 normal)
        {
            if (normal.X > a1) return Direction2.Right;
            if (normal.X < a2) return Direction2.Left;
            if (normal.Y > a1) return Direction2.Up;
            if (normal.Y < a2) return Direction2.Down;
            if (normal.Z > a1) return Direction2.Forward;
            if (normal.Z < a2) return Direction2.Back;

            return Direction2.Up;
        }
    }
}
