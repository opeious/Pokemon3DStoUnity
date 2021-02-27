using System.Collections.Generic;
using System.Xml.Serialization;

namespace P3DS2U.Editor.SPICA.COLLADA
{
    public class DAEAccessor
    {
        [XmlAttribute] public uint count;

        [XmlElement ("param")] public List<DAEAccessorParam> param = new List<DAEAccessorParam> ();
        [XmlAttribute] public string source;
        [XmlAttribute] public uint stride;

        public void AddParam (string name, string type)
        {
            param.Add (new DAEAccessorParam {
                name = name,
                type = type
            });
        }

        public void AddParams (string type, params string[] names)
        {
            foreach (var name in names) AddParam (name, type);
        }
    }

    public class DAEAccessorParam
    {
        [XmlAttribute] public string name;
        [XmlAttribute] public string type;
    }
}