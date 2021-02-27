using System.Numerics;

namespace SPICA.Formats.CtrH3D.Light
{
    public class H3DVertexLight
    {
        public Vector4 AmbientColor;

        public float AttenuationConstant;
        public float AttenuationLinear;
        public float AttenuationQuadratic;
        public Vector4 DiffuseColor;

        public Vector3 Direction;
        public float SpotCutOffAngle;

        public float SpotExponent;
    }
}