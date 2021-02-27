using System.Text;
using System.Xml.Serialization;
using P3DS2U.Editor.SPICA.Math3D;

namespace P3DS2U.Editor.SPICA.COLLADA
{
    public class DAEMatrix
    {
        [XmlText] public string data;
        [XmlAttribute] public string sid;

        public static DAEMatrix Identity => new DAEMatrix {data = "1 0 0 0 0 1 0 0 0 0 1 0 0 0 0 1"};

        public void Set (params Matrix3x4[] Matrices)
        {
            var SB = new StringBuilder ();

            for (var i = 0; i < Matrices.Length; i++)
                if (i < Matrices.Length - 1)
                    SB.Append (DAEUtils.MatrixStr (Matrices[i]) + " ");
                else
                    SB.Append (DAEUtils.MatrixStr (Matrices[i]));

            data = SB.ToString ();
        }
    }
}