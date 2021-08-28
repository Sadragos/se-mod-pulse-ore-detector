using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace PulseOreDetector.Proto
{
    [ProtoContract]
    [Serializable]
    public class MyAlias
    {
        [ProtoMember(1)]
        [XmlAttribute]
        public string Input;
        [ProtoMember(2)]
        [XmlAttribute]
        public string Output;

        public MyAlias(string input, string output)
        {
            Input = input;
            Output = output;
        }

        public MyAlias()
        {
        }
    }
}
