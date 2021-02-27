using System.Numerics;

namespace SPICA.Formats.CtrH3D.Light
{
    public class H3DHemisphereLight
    {
        public Vector3 Direction;
        public Vector4 GroundColor;

        public float LerpFactor;
        public Vector4 SkyColor;
    }
}