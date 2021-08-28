using System;
using System.Collections.Generic;
using ProtoBuf;
using System.Xml.Serialization;
using VRageMath;

namespace PulseOreDetector.Proto
{
    [ProtoContract]
    [Serializable]
    public class MyConfig
    {
        [ProtoMember(1)]
        public int CooldownSeconds = 30;

        [ProtoMember(2)]
        public float MaxRange = 1000;

        [ProtoMember(3)]
        public string GPSFormat = "Res: {0} ({1})";

        [ProtoMember(4)]
        public int MaxChecksPerTick = 50000;

        [ProtoMember(5)]
        public List<MySize> SizeDescriptions = new List<MySize>();

        [ProtoMember(6)]
        public List<MyAlias> OreNames = new List<MyAlias>();

        [ProtoMember(7)]
        public float BatchDistance = 200;

        [ProtoMember(8)]
        public int Resolution = 3;

        [ProtoIgnore]
        public float SafeMaxRange { get { return Math.Min(int.MaxValue - 1, MaxRange); } }
    }
}
