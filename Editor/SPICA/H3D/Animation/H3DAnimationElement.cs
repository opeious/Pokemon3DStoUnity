using P3DS2U.Editor.SPICA.Serialization.Attributes;

namespace P3DS2U.Editor.SPICA.H3D.Animation
{
    public class H3DAnimationElement : INamed
    {
        [Inline]
        [TypeChoiceName ("PrimitiveType")]
        [TypeChoice ((uint) H3DPrimitiveType.Float, typeof(H3DAnimFloat))]
        [TypeChoice ((uint) H3DPrimitiveType.Vector2D, typeof(H3DAnimVector2D))]
        [TypeChoice ((uint) H3DPrimitiveType.Vector3D, typeof(H3DAnimVector3D))]
        [TypeChoice ((uint) H3DPrimitiveType.Transform, typeof(H3DAnimTransform))]
        [TypeChoice ((uint) H3DPrimitiveType.RGBA, typeof(H3DAnimRGBA))]
        [TypeChoice ((uint) H3DPrimitiveType.Texture, typeof(H3DAnimFloat))]
        [TypeChoice ((uint) H3DPrimitiveType.QuatTransform, typeof(H3DAnimQuatTransform))]
        [TypeChoice ((uint) H3DPrimitiveType.Boolean, typeof(H3DAnimBoolean))]
        [TypeChoice ((uint) H3DPrimitiveType.MtxTransform, typeof(H3DAnimMtxTransform))]
        private object _Content;

        public H3DPrimitiveType PrimitiveType;

        public H3DTargetType TargetType;

        public object Content {
            get => _Content;
            set {
                var ValueType = value.GetType ();

                if (ValueType != typeof(H3DAnimFloat) &&
                    ValueType != typeof(H3DAnimVector2D) &&
                    ValueType != typeof(H3DAnimVector3D) &&
                    ValueType != typeof(H3DAnimTransform) &&
                    ValueType != typeof(H3DAnimRGBA) &&
                    ValueType != typeof(H3DAnimQuatTransform) &&
                    ValueType != typeof(H3DAnimBoolean) &&
                    ValueType != typeof(H3DAnimMtxTransform)) {
                }

                _Content = value;
            }
        }

        public string Name { get; set; }
    }
}