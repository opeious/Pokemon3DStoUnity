using System.Xml.Serialization;

namespace P3DS2U.Editor.SPICA.COLLADA
{
    public class DAEMaterial
    {
        [XmlAttribute] public string id;

        public DAEInstanceEffect instance_effect = new DAEInstanceEffect ();
        [XmlAttribute] public string name;
    }

    public class DAEInstanceEffect
    {
        [XmlAttribute] public string url;
    }
}