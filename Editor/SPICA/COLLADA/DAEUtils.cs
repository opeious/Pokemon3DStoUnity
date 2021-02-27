using System;
using System.Globalization;
using P3DS2U.Editor.SPICA.Math3D;
using UnityEngine;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

namespace P3DS2U.Editor.SPICA.COLLADA
{
    internal static class DAEUtils
    {
        private const float RadToDegConstant = (float) (1 / Math.PI * 180);

        private const string Matrix3x4Format = "{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11} 0 0 0 1";

        public static Vector3 ToUnityVector3 (DAEVector3 daeVector3)
        {
            var splits = daeVector3.data.Split (' ');
            float.TryParse (splits[0], out var x);
            float.TryParse (splits[1], out var y);
            float.TryParse (splits[2], out var z);
            return new Vector3 (x, y, z);
        }

        public static float RadToDeg (float Radians)
        {
            return Radians * RadToDegConstant;
        }

        public static string RadToDegStr (float Radians)
        {
            return RadToDeg (Radians).ToString (CultureInfo.InvariantCulture);
        }

        public static string VectorStr (Vector2 v)
        {
            return FormattableString.Invariant ($"{v.X} {v.Y}");
        }

        public static string VectorStr (System.Numerics.Vector3 v)
        {
            return FormattableString.Invariant ($"{v.X} {v.Y} {v.Z}");
        }

        public static string VectorStr (Vector4 v)
        {
            return FormattableString.Invariant ($"{v.X} {v.Y} {v.Z} {v.W}");
        }

        public static string Vector2Str (Vector4 v)
        {
            return FormattableString.Invariant ($"{v.X} {v.Y}");
        }

        public static string Vector3Str (Vector4 v)
        {
            return FormattableString.Invariant ($"{v.X} {v.Y} {v.Z}");
        }

        public static string Vector4Str (Vector4 v)
        {
            return VectorStr (v);
        }

        public static string MatrixStr (Matrix3x4 m)
        {
            return string.Format (CultureInfo.InvariantCulture, Matrix3x4Format,
                m.M11, m.M21, m.M31, m.M41,
                m.M12, m.M22, m.M32, m.M42,
                m.M13, m.M23, m.M33, m.M43);
        }
    }
}