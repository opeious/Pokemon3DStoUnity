using P3DS2U.Editor.SPICA.Serialization;
using P3DS2U.Editor.SPICA.Serialization.Attributes;

namespace P3DS2U.Editor.SPICA.H3D.Animation
{
    public class H3DAnimRGBA : ICustomSerialization
    {
        [Ignore] private readonly H3DFloatKeyFrameGroup[] Vector;

        public H3DAnimRGBA ()
        {
            Vector = new[] {
                new H3DFloatKeyFrameGroup (),
                new H3DFloatKeyFrameGroup (),
                new H3DFloatKeyFrameGroup (),
                new H3DFloatKeyFrameGroup ()
            };
        }

        public H3DFloatKeyFrameGroup R => Vector[0];
        public H3DFloatKeyFrameGroup G => Vector[1];
        public H3DFloatKeyFrameGroup B => Vector[2];
        public H3DFloatKeyFrameGroup A => Vector[3];

        void ICustomSerialization.Deserialize (BinaryDeserializer Deserializer)
        {
            H3DAnimVector.SetVector (Deserializer, Vector);
        }

        bool ICustomSerialization.Serialize (BinarySerializer Serializer)
        {
            H3DAnimVector.WriteVector (Serializer, Vector);

            return true;
        }
    }
}