using System.Xml.Serialization;

namespace P3DS2U.Editor.SPICA.COLLADA
{
    public class DAEArray
    {
        [XmlAttribute] public uint count;

        [XmlText] public string data;
        [XmlAttribute] public string id;
    }
}