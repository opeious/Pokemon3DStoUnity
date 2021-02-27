using P3DS2U.Editor.SPICA.Serialization;
using P3DS2U.Editor.SPICA.Serialization.Attributes;

namespace P3DS2U.Editor.SPICA.H3D
{
    public class H3DStringUtf16 : ICustomSerialization
    {
        [Ignore] private string Str;

        public H3DStringUtf16 ()
        {
        }

        public H3DStringUtf16 (string Str)
        {
            this.Str = Str;
        }

        void ICustomSerialization.Deserialize (BinaryDeserializer Deserializer)
        {
            Str = Deserializer.Reader.ReadNullTerminatedStringUtf16LE ();
        }

        bool ICustomSerialization.Serialize (BinarySerializer Serializer)
        {
            Serializer.Writer.WriteNullTerminatedStringUtf16LE (Str);

            return true;
        }

        public override string ToString ()
        {
            return Str ?? string.Empty;
        }
    }
}