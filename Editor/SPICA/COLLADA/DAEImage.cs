using System.Xml.Serialization;

namespace SPICA.Formats.Generic.COLLADA
{
    public class DAEImage
    {
        [XmlAttribute] public string id;

        public string init_from;
        [XmlAttribute] public string name;
    }
}