using System;
using System.Numerics;
using P3DS2U.Editor.SPICA.Serialization.Attributes;

namespace P3DS2U.Editor.SPICA.H3D.Camera
{
    public class H3DCamera : INamed
    {
        private string _Name;

        public string Name
        {
            get => _Name;
            set => _Name = value ?? throw new Exception ("null");
        }

        public Vector3 TransformScale;
        public Vector3 TransformRotation;
        public Vector3 TransformTranslation;

        public H3DCameraViewType ViewType;

        public H3DCameraProjectionType ProjectionType;

        [Padding(2)] public H3DCameraFlags Flags;

        public float WScale;

        [TypeChoiceName("ViewType")]
        [TypeChoice((uint)H3DCameraViewType.Aim,    typeof(H3DCameraViewAim))]
        [TypeChoice((uint)H3DCameraViewType.LookAt, typeof(H3DCameraViewLookAt))]
        [TypeChoice((uint)H3DCameraViewType.Rotate, typeof(H3DCameraViewRotation))]
        public object View;

        [TypeChoiceName("ProjectionType")]
        [TypeChoice((uint)H3DCameraProjectionType.Perspective, typeof(H3DCameraProjectionPerspective))]
        [TypeChoice((uint)H3DCameraProjectionType.Orthogonal,  typeof(H3DCameraProjectionOrthogonal))]
        public object Projection;

        public H3DMetaData MetaData;
    }
}