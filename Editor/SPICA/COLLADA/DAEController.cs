using System.Xml.Serialization;

namespace P3DS2U.Editor.SPICA.COLLADA
{
    public class DAEController
    {
        [XmlAttribute] public string id;
        [XmlAttribute] public string name;

        public DAESkin skin = new DAESkin ();
    }
}