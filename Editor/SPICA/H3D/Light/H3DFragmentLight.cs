using System.Numerics;
using P3DS2U.Editor.SPICA.Math3D;

namespace P3DS2U.Editor.SPICA.H3D.Light
{
    public class H3DFragmentLight
    {
        public RGBA AmbientColor;
        public RGBA DiffuseColor;
        public RGBA Specular0Color;
        public RGBA Specular1Color;

        public Vector3 Direction;

        private uint DistanceSamplerPtr;
        private uint AngleSamplerPtr;

        public float AttenuationStart;
        public float AttenuationEnd;

        public string DistanceLUTTableName;
        public string DistanceLUTSamplerName;

        public string AngleLUTTableName;
        public string AngleLUTSamplerName;
    }
}