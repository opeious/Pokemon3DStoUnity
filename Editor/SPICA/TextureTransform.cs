using System;
using System.Numerics;
using P3DS2U.Editor.SPICA.Math3D;

namespace P3DS2U.Editor.SPICA
{
    internal static class TextureTransform
    {
        public static Matrix3x4 GetTransform (Vector2 S, float R, Vector2 T, TextureTransformType TT)
        {
            var Transform = Matrix3x4.Identity;

            var SX = S.X;
            var SY = S.Y;

            var TX = T.X;
            var TY = T.Y;

            var CA = (float) Math.Cos (R);
            var SA = (float) Math.Sin (R);

            Transform.M11 = SX * CA;
            Transform.M12 = SY * SA;
            Transform.M21 = SX * -SA;
            Transform.M22 = SY * CA;

            switch (TT) {
                case TextureTransformType.DccMaya:
                    Transform.M41 = SX * (0.5f * SA - 0.5f * CA + 0.5f - TX);
                    Transform.M42 = SY * (0.5f * -SA - 0.5f * CA + 0.5f - TY);
                    break;

                case TextureTransformType.DccSoftImage:
                    Transform.M41 = SX * (-CA * TX - SA * TY);
                    Transform.M42 = SY * (SA * TX - CA * TY);
                    break;

                case TextureTransformType.Dcc3dsMax:
                    Transform.M41 =
                        SX * CA * (-TX - 0.5f) -
                        SX * SA * (TY - 0.5f) + 0.5f;
                    Transform.M42 =
                        SY * SA * (-TX - 0.5f) +
                        SY * CA * (TY - 0.5f) + 0.5f;
                    break;
            }

            return Transform;
        }
    }
}