using Lidgren.Network;
using System.Numerics;

namespace SpaceNetwork
{
    public static class NetExtensions
    {
        public static void WriteObject(this NetOutgoingMessage msg, object obj)
        {
            if (obj == null) { msg.Write((byte)0); return; }

            if (obj is int i) { msg.Write((byte)1); msg.Write(i); }
            else if (obj is float f) { msg.Write((byte)2); msg.Write(f); }
            else if (obj is string s) { msg.Write((byte)3); msg.Write(s); }
            else if (obj is bool b) { msg.Write((byte)4); msg.Write(b); }
            else if (obj is Vector2 v2) { msg.Write((byte)5); msg.Write(v2.X); msg.Write(v2.Y); }
            else if (obj is Vector3 v3) { msg.Write((byte)6); msg.Write(v3.X); msg.Write(v3.Y); msg.Write(v3.Z); }
            else if (obj is Quaternion q) { msg.Write((byte)7); msg.Write(q.X); msg.Write(q.Y); msg.Write(q.Z); msg.Write(q.W); }
            else if (obj is byte by) { msg.Write((byte)8); msg.Write(by); }
            else if (obj is short sh) { msg.Write((byte)9); msg.Write(sh); }
            else if (obj is Vector4 v4) { msg.Write((byte)10); msg.Write(v4.X); msg.Write(v4.Y); msg.Write(v4.Z); msg.Write(v4.W); }

            else { msg.Write((byte)0); }
        }
        public static T ReadObject<T>(this NetIncomingMessage msg)
        {
            object obj = msg.ReadObject();

            if (obj == null) return default(T);

            return (T)obj;
        }


        public static object ReadObject(this NetIncomingMessage msg)
        {
            byte typeId = msg.ReadByte();
            switch (typeId)
            {
                case 0: return null;
                case 1: return msg.ReadInt32();
                case 2: return msg.ReadFloat();
                case 3: return msg.ReadString();
                case 4: return msg.ReadBoolean();
                case 5: return new Vector2(msg.ReadFloat(), msg.ReadFloat());
                case 6: return new Vector3(msg.ReadFloat(), msg.ReadFloat(), msg.ReadFloat());
                case 7: return new Quaternion(msg.ReadFloat(), msg.ReadFloat(), msg.ReadFloat(), msg.ReadFloat());
                case 8: return msg.ReadByte();
                case 9: return msg.ReadInt16();
                case 10: return new Vector4(msg.ReadFloat(), msg.ReadFloat(), msg.ReadFloat(), msg.ReadFloat());
                default: return null;
            }
        }
    }
}