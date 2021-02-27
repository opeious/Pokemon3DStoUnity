using System.Xml.Serialization;

namespace SPICA.Formats.Generic.COLLADA
{
    public class DAEGeometry
    {
        [XmlAttribute] public string id;

        public DAEMesh mesh = new DAEMesh ();
        [XmlAttribute] public string name;
    }
}