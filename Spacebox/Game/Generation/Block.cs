
using System.Runtime.CompilerServices;
using OpenTK.Mathematics;
using Engine;
using Spacebox.Game.Resources;

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

    [Flags]
    public enum BlockFlags : byte
    {
        None = 0,
        Transparent = 1 << 0,
        Air = 1 << 1,
        Light = 1 << 2,
        EnableEmission = 1 << 3
    }

    public class Block
    {
        private long data;

        // [0..11]   = BlockId (12 bit, short)
        // [12..14]  = Direction (3 bit)
        // [15..22]  = Mass (8 bit)
        // [23..30]  = Durability (8 bit)
        // [31..34]  = Flags (4 bit)
        //           = 35 bit

        public Vector3 Color { get; set; } = Vector3.One;
        public Vector3 LightColor { get; set; } = Vector3.Zero;
        public float LightLevel { get; set; }

        public Block(){}

        public Block(short blockId, Direction dir, byte mass, byte durability,
                     bool isTransparent, bool isAir, bool isLight, bool enableEmission)
        {
            SetBlockId(blockId);
            SetDirection(dir);
            SetMass(mass);
            SetDurability(durability);
            SetTransparent(isTransparent);
            SetAir(isAir);
            SetLight(isLight);
            SetEnableEmission(enableEmission);
        }

        public Block(BlockData blockData)
        {
            BlockId = blockData.Id;
            LightColor = blockData.LightColor;
            SetBlockId(blockData.Id);
            SetDirection(Direction.Up);
            SetMass(blockData.Mass);
            SetDurability(blockData.Durability);
            SetTransparent(blockData.IsTransparent);
            SetAir(blockData.Id == 0);

            SetEnableEmission(true);

            if (LightColor != Vector3.Zero)
            {
                LightLevel = 15;
                SetLight(true);
            }
            else
            {
                SetLight(false);
            }
        }

        public short BlockId
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (short)(data & 0xFFF);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                long v = (value & 0xFFF);
                data = (data & ~0xFFFL) | v;
            }
        }
        public void SetBlockId(short id) => BlockId = id;

        public Direction Direction
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (Direction)((data >> 12) & 0b111);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                long v = ((long)value & 0b111) << 12;
                data = (data & ~(0b111L << 12)) | v;
            }
        }
        public void SetDirection(Direction dir) => Direction = dir;

        public byte Mass
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (byte)((data >> 15) & 0xFF);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                long v = ((long)value & 0xFF) << 15;
                data = (data & ~(0xFFL << 15)) | v;
            }
        }
        public void SetMass(byte mass) => Mass = mass;

        public byte Durability
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (byte)((data >> 23) & 0xFF);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                long v = ((long)value & 0xFF) << 23;
                data = (data & ~(0xFFL << 23)) | v;
            }
        }
        public void SetDurability(byte dur) => Durability = dur;

        private BlockFlags Flags
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (BlockFlags)((data >> 31) & 0xF);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                long v = ((long)value & 0xF) << 31;
                data = (data & ~(0xFL << 31)) | v;
            }
        }

        public bool IsTransparent
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (Flags & BlockFlags.Transparent) != 0;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                var f = Flags;
                if (value) f |= BlockFlags.Transparent; else f &= ~BlockFlags.Transparent;
                Flags = f;
            }
        }
        public void SetTransparent(bool val) => IsTransparent = val;

        public bool IsAir
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (Flags & BlockFlags.Air) != 0;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                var f = Flags;
                if (value) f |= BlockFlags.Air; else f &= ~BlockFlags.Air;
                Flags = f;
            }
        }
        public void SetAir(bool val) => IsAir = val;

        public bool IsLight
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (Flags & BlockFlags.Light) != 0;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                var f = Flags;
                if (value) f |= BlockFlags.Light; else f &= ~BlockFlags.Light;
                Flags = f;
            }
        }
        public void SetLight(bool val) => IsLight = val;

        public bool EnableEmission
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (Flags & BlockFlags.EnableEmission) != 0;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                var f = Flags;
                if (value) f |= BlockFlags.EnableEmission; else f &= ~BlockFlags.EnableEmission;
                Flags = f;
            }
        }
        public void SetEnableEmission(bool val) => EnableEmission = val;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Direction GetDirectionFromNormal(Vector3SByte normal)
        {
            return GetDirectionFromNormal(new Vector3(normal.X, normal.Y, normal.Z));
        }
        public static Direction GetDirectionFromNormal(Vector3 normal)
        {
            const float eps = 1e-6f;
            float a1 = 1f - eps;
            float a2 = -1f + eps;
            if (normal.X > a1) return Direction.Right;
            if (normal.X < a2) return Direction.Left;
            if (normal.Y > a1) return Direction.Up;
            if (normal.Y < a2) return Direction.Down;
            if (normal.Z > a1) return Direction.Forward;
            if (normal.Z < a2) return Direction.Back;
            return Direction.Up;
        }

        public Vector3 GetVectorFromDirection()
        {
            return GetVectorFromDirection(Direction);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 GetVectorFromDirection(Direction direction)
        {
            return direction switch
            {
                Direction.Up => Vector3.UnitY,
                Direction.Down => -Vector3.UnitY,
                Direction.Left => -Vector3.UnitX,
                Direction.Right => Vector3.UnitX,
                Direction.Forward => Vector3.UnitZ,
                Direction.Back => -Vector3.UnitZ,
                _ => Vector3.Zero
            };
        }

        public void SetDirectionFromNormal(Vector3 normal)
        {
            Direction = GetDirectionFromNormal(normal);
        }

        public void SetDirectionFromNormal(Vector3SByte normal)
        {
            Direction = GetDirectionFromNormal(normal);
        }


        public bool Is<T>() where T : Block => this is T;
        public bool Is<T>(out T res) where T : Block
        {
            res = this as T;
            return res != null;
        }

        public override string ToString()
        {
            return $"ID={BlockId}, Dir={Direction}, Mass={Mass}, Dur={Durability}, \nFlags=[T={IsTransparent}, A={IsAir}, L={IsLight}, E={EnableEmission}], " +
                   $"\nCol={RoundVector3(Color)}, LCol={RoundVector3(LightColor)}, LLvl={RoundFloat(LightLevel)}";
        }

        public static float RoundFloat(float x)
        {
            return (float)Math.Round(x, MidpointRounding.ToEven);
        }

        public static Vector3 RoundVector3(Vector3 v)
        {
            return new Vector3(RoundFloat(v.X), RoundFloat(v.Y), RoundFloat(v.Z));
        }
    }
}
