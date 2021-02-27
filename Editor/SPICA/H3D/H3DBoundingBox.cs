using System.Numerics;
using P3DS2U.Editor.SPICA.Math3D;

namespace P3DS2U.Editor.SPICA.H3D
{
    public struct H3DBoundingBox
    {
        public Vector3 Center;
        public Matrix3x3 Orientation;
        public Vector3 Size;
    }
}