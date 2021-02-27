using System.Xml.Serialization;

namespace P3DS2U.Editor.SPICA.COLLADA
{
    public class DAEInput
    {
        [XmlAttribute] public string semantic;
        [XmlAttribute] public string source;
    }
}