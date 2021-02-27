using System.Xml.Serialization;

namespace SPICA.Formats.Generic.COLLADA
{
    public class DAEInputOffset
    {
        [XmlAttribute] public uint offset;
        [XmlAttribute] public string semantic;
        [XmlAttribute] public uint set;
        [XmlAttribute] public string source;

        public bool ShouldSerializeset ()
        {
            return semantic == "TEXCOORD" || set != 0;
        }
    }
}