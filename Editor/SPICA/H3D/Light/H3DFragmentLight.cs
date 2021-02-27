using System.Numerics;
using P3DS2U.Editor.SPICA.Math3D;

namespace P3DS2U.Editor.SPICA.H3D.Light
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