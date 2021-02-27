using System.Collections.Generic;
using System.Xml.Serialization;

namespace P3DS2U.Editor.SPICA.COLLADA
{
    public class DAESkin
    {
        public DAEMatrix bind_shape_matrix = DAEMatrix.Identity;

        public DAEJoints joints = new DAEJoints ();
        [XmlAttribute] public string source;

        [XmlElement ("source")] public List<DAESource> src = new List<DAESource> ();
        public DAEWeights vertex_weights = new DAEWeights ();
    }

    public class DAEJoints
    {
        [XmlElement ("input")] public List<DAEInput> input = new List<DAEInput> ();

        public void AddInput (string semantic, string source)
        {
            input.Add (new DAEInput {
                semantic = semantic,
                source = source
            });
        }
    }

    public class DAEWeights
    {
        [XmlAttribute] public uint count;

        [XmlElement ("input")] public List<DAEInputOffset> input = new List<DAEInputOffset> ();
        public string v;

        public string vcount;

        public void AddInput (string semantic, string source, uint offset = 0)
        {
            input.Add (new DAEInputOffset {
                semantic = semantic,
                source = source,
                offset = offset
            });
        }
    }
}