using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace PulseOreDetector.Proto
{
    [ProtoContract]
    class MyMessageObject
    {
        [ProtoMember(1)]
        public ulong SteamId;
        [ProtoMember(2)]
        public MyMessageType Type;
        [ProtoMember(3)]
        public MyConfig Config;
    }

    public enum MyMessageType
    {
        RequesetConfig,
        SupplyConfig
    }
}
