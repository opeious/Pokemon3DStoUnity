using System;
using SPICA.Formats.CtrH3D.Texture;
using SPICA.Serialization.Serializer;

namespace SPICA.Formats.CtrH3D
{
    internal static class H3DComparers
    {
        public static Comparison<RefValue> GetComparisonStr ()
        {
            return CompareString;
        }

        public static Comparison<RefValue> GetComparisonRaw ()
        {
            return CompareBuffer;
        }

        public static int CompareString (RefValue LHS, RefValue RHS)
        {
            return CompareString (LHS.Value.ToString (), RHS.Value.ToString ());
        }

        public static int CompareBuffer (RefValue LHS, RefValue RHS)
        {
            if (LHS.Parent == RHS.Parent)
                return 0;
            if (LHS.Parent is H3DTexture)
                return -1;
            return 1;
        }

        public static int CompareString (string LHS, string RHS)
        {
            var MinLength = Math.Min (LHS.Length, RHS.Length);

            for (var Index = 0; Index < MinLength; Index++) {
                var L = (byte) LHS[Index];
                var R = (byte) RHS[Index];

                if (L != R) return L < R ? -1 : 1;
            }

            if (LHS.Length == RHS.Length)
                return 0;
            if (LHS.Length < RHS.Length)
                return -1;
            return 1;
        }
    }
}