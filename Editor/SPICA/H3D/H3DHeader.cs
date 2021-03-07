using P3DS2U.Editor.SPICA.Serialization;
using P3DS2U.Editor.SPICA.Serialization.Attributes;

namespace P3DS2U.Editor.SPICA.H3D
{
    class H3DHeader
    {
        [Inline]
        public string Magic;

        [Version]
        public byte BackwardCompatibility;
        public byte ForwardCompatibility;

        public ushort ConverterVersion;

        public int ContentsAddress;
        public int StringsAddress;
        public int CommandsAddress;
        public int RawDataAddress;

        [IfVersion(CmpOp.Gequal, 0x21)]
        public int RawExtAddress;

        public int RelocationAddress;

        public int ContentsLength;
        public int StringsLength;
        public int CommandsLength;
        public int RawDataLength;

        [IfVersion(CmpOp.Gequal, 0x21)]
        public int RawExtLength;

        public int RelocationLength;

        public int UnInitDataLength;
        public int UnInitCommandsLength;

        [IfVersion(CmpOp.Gequal, 8), Padding(2)] public H3DFlags Flags;

        [IfVersion(CmpOp.Gequal, 8)] public ushort AddressCount;

        public H3DHeader()
        {
            Magic = "BCH";
        }
    }
}