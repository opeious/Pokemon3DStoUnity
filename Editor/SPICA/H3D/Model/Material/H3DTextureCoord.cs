using System.Numerics;
using P3DS2U.Editor.SPICA.Math3D;

namespace P3DS2U.Editor.SPICA.H3D.Model.Material
{
    public struct H3DTextureCoord
    {
        public H3DTextureCoordFlags    Flags;
        public H3DTextureTransformType TransformType;
        public H3DTextureMappingType   MappingType;

        public sbyte ReferenceCameraIndex;

        public Vector2 Scale;
        public float   Rotation;
        public Vector2 Translation;

        public Matrix3x4 GetTransform()
        {
            return TextureTransform.GetTransform(
                Scale,
                Rotation,
                Translation,
                (TextureTransformType)TransformType);
        }
    }
}