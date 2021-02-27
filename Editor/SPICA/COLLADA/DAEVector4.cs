using System.Numerics;
using System.Xml.Serialization;

namespace P3DS2U.Editor.SPICA.COLLADA
{
    public class DAEVector4
    {
        [XmlText] public string data;
        [XmlAttribute] public string sid;

        public static DAEVector4 Empty => new DAEVector4 {data = "0 0 0 0"};

        public void Set (Vector4 Vector)
        {
            data = DAEUtils.VectorStr (Vector);
        }
    }
}