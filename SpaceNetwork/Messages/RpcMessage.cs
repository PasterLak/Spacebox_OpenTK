using System;
using Lidgren.Network;

namespace SpaceNetwork.Messages
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RpcAttribute : Attribute { }

    public class RpcMessage : BaseMessage
    {
        public int NetworkId { get; set; }
        public int TargetId { get; set; } = -1; // -1 eveyone
        public string MethodName { get; set; }
        public object[] Parameters { get; set; }

        protected override void WriteData(NetOutgoingMessage om)
        {
           
            om.Write(NetworkId);
            om.Write(MethodName ?? "");

            int count = (Parameters != null) ? Parameters.Length : 0;
            om.Write(count);

            for (int i = 0; i < count; i++)
            {
                om.WriteObject(Parameters[i]);
            }
        }

        public override void Read(NetIncomingMessage im)
        {
            NetworkId = im.ReadInt32();
            MethodName = im.ReadString();

            int count = im.ReadInt32();
            Parameters = new object[count];
            for (int i = 0; i < count; i++)
            {
                Parameters[i] = im.ReadObject();
            }
        }
    }
}