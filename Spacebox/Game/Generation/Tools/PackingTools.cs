namespace Spacebox.Game.Generation.Tools
{
    public class PackingTools
    {

        // ------------------------- PACK --------------------------- // 
        public static short PackBytes(byte high, byte low)
        {
            return (short)(high << 8 | low);
        }

        public static int PackShorts(short a, short b)
        {
            return (ushort)a << 16 | (ushort)b;
        }

        public static long PackShorts(short a, short b, short c, short d)
        {
            return (long)(ushort)a << 48 |
                   (long)(ushort)b << 32 |
                   (long)(ushort)c << 16 |
                   (ushort)d;
        }

        public static long PackInts(int high, int low)
        {
            return (long)(uint)high << 32 | (uint)low;
        }


        // ------------------------- UNPACK --------------------------- // 

        public static void UnpackBytes(short packed, out byte high, out byte low)
        {
            high = (byte)(packed >> 8 & 0xFF);
            low = (byte)(packed & 0xFF);
        }

        public static void UnpackShorts(int packed, out short a, out short b)
        {
            a = (short)(packed >> 16 & 0xFFFF);
            b = (short)(packed & 0xFFFF);
        }


        public static void UnpackShorts(long packed, out short a, out short b, out short c, out short d)
        {
            a = (short)(packed >> 48 & 0xFFFF);
            b = (short)(packed >> 32 & 0xFFFF);
            c = (short)(packed >> 16 & 0xFFFF);
            d = (short)(packed & 0xFFFF);
        }

        public static void UnpackInts(long packed, out int high, out int low)
        {
            high = (int)(packed >> 32);
            low = (int)(packed & 0xFFFFFFFF);
        }

        // ------------------------- CONVERT --------------------------- // 

        public static byte SByteToByte(sbyte value)
        {
            return unchecked((byte)value);
        }
        public static sbyte ByteToSByte(byte value)
        {
            return unchecked((sbyte)value);
        }
    }
}
