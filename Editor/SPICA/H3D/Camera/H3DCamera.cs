using System.Numerics;
using P3DS2U.Editor.SPICA.Serialization.Attributes;

namespace P3DS2U.Editor.SPICA.H3D.Camera
{
    public class H3DCamera : INamed
    {
        [Padding (2)] public H3DCameraFlags Flags;

        public H3DMetaData MetaData;

        [TypeChoiceName ("ProjectionType")]
        [TypeChoice ((uint) H3DCameraProjectionType.Perspective, typeof(H3DCameraProjectionPerspective))]
        [TypeChoice ((uint) H3DCameraProjectionType.Orthogonal, typeof(H3DCameraProjectionOrthogonal))]
        public object Projection;

        public H3DCameraProjectionType ProjectionType;
        public Vector3 TransformRotation;

        public Vector3 TransformScale;
        public Vector3 TransformTranslation;

        [TypeChoiceName ("ViewType")]
        [TypeChoice ((uint) H3DCameraViewType.Aim, typeof(H3DCameraViewAim))]
        [TypeChoice ((uint) H3DCameraViewType.LookAt, typeof(H3DCameraViewLookAt))]
        [TypeChoice ((uint) H3DCameraViewType.Rotate, typeof(H3DCameraViewRotation))]
        public object View;

        public H3DCameraViewType ViewType;

        public float WScale;

        public string Name { get; set; }
    }
}