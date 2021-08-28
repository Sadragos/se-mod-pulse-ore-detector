using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace PulseOreDetector.Proto
{
    [ProtoContract]
    [Serializable]
    public class MySize
    {
        [ProtoMember(1)]
        [XmlAttribute]
        public int MinSize;

        [ProtoMember(2)]
        [XmlAttribute]
        public string Name;

        public MySize()
        {
        }

        public MySize(int minSize, string name)
        {
            MinSize = minSize;
            Name = name;
        }
    }
}
