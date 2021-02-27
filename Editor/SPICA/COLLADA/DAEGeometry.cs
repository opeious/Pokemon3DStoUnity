using System.Xml.Serialization;

namespace P3DS2U.Editor.SPICA.COLLADA
{
    public class DAEGeometry
    {
        [XmlAttribute] public string id;

        public DAEMesh mesh = new DAEMesh ();
        [XmlAttribute] public string name;
    }
}