using System.Numerics;

namespace P3DS2U.Editor.SPICA.H3D.Light
{
    public class H3DVertexLight
    {
        public Vector4 AmbientColor;
        public Vector4 DiffuseColor;

        public Vector3 Direction;

        public float AttenuationConstant;
        public float AttenuationLinear;
        public float AttenuationQuadratic;

        public float SpotExponent;
        public float SpotCutOffAngle;
    }
}