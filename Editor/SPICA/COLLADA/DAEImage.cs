using System.Xml.Serialization;

namespace P3DS2U.Editor.SPICA.COLLADA
{
    public class DAEImage
    {
        [XmlAttribute] public string id;

        public string init_from;
        [XmlAttribute] public string name;
    }
}