using P3DS2U.Editor.SPICA.Serialization;
using P3DS2U.Editor.SPICA.Serialization.Attributes;

namespace P3DS2U.Editor.SPICA.H3D.Animation
{
    public class H3DAnimFloat : ICustomSerialization
    {
        [Ignore] private H3DFloatKeyFrameGroup _Value;

        public H3DAnimFloat ()
        {
            _Value = new H3DFloatKeyFrameGroup ();
        }

        public H3DFloatKeyFrameGroup Value => _Value;

        void ICustomSerialization.Deserialize (BinaryDeserializer Deserializer)
        {
            H3DAnimVector.SetVector (Deserializer, ref _Value);
        }

        bool ICustomSerialization.Serialize (BinarySerializer Serializer)
        {
            H3DAnimVector.WriteVector (Serializer, _Value);

            return true;
        }
    }
}