using System.Xml.Serialization;

namespace SPICA.Formats.Generic.COLLADA
{
    public class DAEArray
    {
        [XmlAttribute] public uint count;

        [XmlText] public string data;
        [XmlAttribute] public string id;
    }
}