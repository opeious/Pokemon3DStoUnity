using P3DS2U.Editor.SPICA.Serialization;
using P3DS2U.Editor.SPICA.Serialization.Attributes;

namespace P3DS2U.Editor.SPICA.H3D.Animation
{
    public class H3DAnimVector2D : ICustomSerialization
    {
        [Ignore] private H3DFloatKeyFrameGroup[] Vector;

        public H3DFloatKeyFrameGroup X => Vector[0];
        public H3DFloatKeyFrameGroup Y => Vector[1];

        public H3DAnimVector2D()
        {
            Vector = new H3DFloatKeyFrameGroup[]
            {
                new H3DFloatKeyFrameGroup(),
                new H3DFloatKeyFrameGroup()
            };
        }

        void ICustomSerialization.Deserialize(BinaryDeserializer Deserializer)
        {
            H3DAnimVector.SetVector(Deserializer, Vector);
        }

        bool ICustomSerialization.Serialize(BinarySerializer Serializer)
        {
            H3DAnimVector.WriteVector(Serializer, Vector);

            return true;
        }
    }
}