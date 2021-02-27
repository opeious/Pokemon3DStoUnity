using System.Numerics;
using SPICA.Math3D;

namespace SPICA.Formats.CtrH3D.Light
{
    public class H3DFragmentLight
    {
        public RGBA AmbientColor;
        public string AngleLUTSamplerName;

        public string AngleLUTTableName;
        private uint AngleSamplerPtr;
        public float AttenuationEnd;

        public float AttenuationStart;
        public RGBA DiffuseColor;

        public Vector3 Direction;
        public string DistanceLUTSamplerName;

        public string DistanceLUTTableName;

        private uint DistanceSamplerPtr;
        public RGBA Specular0Color;
        public RGBA Specular1Color;
    }
}