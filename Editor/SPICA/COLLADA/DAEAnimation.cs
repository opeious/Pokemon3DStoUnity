using System.Collections.Generic;
using System.Xml.Serialization;

namespace P3DS2U.Editor.SPICA.COLLADA
{
    public class DAEAnimation
    {
        public DAEChannel channel = new DAEChannel ();
        [XmlAttribute] public string id;
        [XmlAttribute] public string name;

        public DAESamplers sampler = new DAESamplers ();

        [XmlElement ("source")] public List<DAESource> src = new List<DAESource> ();
    }

    public class DAESamplers
    {
        [XmlAttribute] public string id;

        [XmlElement ("input")] public List<DAEInput> input = new List<DAEInput> ();

        public void AddInput (string semantic, string source)
        {
            input.Add (new DAEInput {
                semantic = semantic,
                source = source
            });
        }
    }

    public class DAEChannel
    {
        [XmlAttribute] public string source;
        [XmlAttribute] public string target;
    }
}