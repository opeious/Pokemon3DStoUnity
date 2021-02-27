using System.Numerics;
using System.Xml.Serialization;

namespace P3DS2U.Editor.SPICA.COLLADA
{
    public class DAEVector3
    {
        [XmlText] public string data;
        [XmlAttribute] public string sid;

        public static DAEVector3 Empty => new DAEVector3 {data = "0 0 0"};

        public void Set (Vector3 Vector)
        {
            data = DAEUtils.VectorStr (Vector);
        }
    }
}