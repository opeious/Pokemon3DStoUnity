using P3DS2U.Editor.SPICA.Serialization;
using P3DS2U.Editor.SPICA.Serialization.Attributes;

namespace P3DS2U.Editor.SPICA.H3D
{
    internal class H3DHeader
    {
        [IfVersion (CmpOp.Gequal, 8)] public ushort AddressCount;

        [Version] public byte BackwardCompatibility;

        public int CommandsAddress;
        public int CommandsLength;

        public int ContentsAddress;

        public int ContentsLength;

        public ushort ConverterVersion;

        [IfVersion (CmpOp.Gequal, 8)] [Padding (2)]
        public H3DFlags Flags;

        public byte ForwardCompatibility;

        [Inline] public string Magic;

        public int RawDataAddress;
        public int RawDataLength;

        [IfVersion (CmpOp.Gequal, 0x21)] public int RawExtAddress;

        [IfVersion (CmpOp.Gequal, 0x21)] public int RawExtLength;

        public int RelocationAddress;

        public int RelocationLength;
        public int StringsAddress;
        public int StringsLength;
        public int UnInitCommandsLength;

        public int UnInitDataLength;

        public H3DHeader ()
        {
            Magic = "BCH";
        }
    }
}