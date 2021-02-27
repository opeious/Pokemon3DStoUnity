using P3DS2U.Editor.SPICA.PICA;
using P3DS2U.Editor.SPICA.Serialization;
using P3DS2U.Editor.SPICA.Serialization.Attributes;

namespace P3DS2U.Editor.SPICA.H3D.LUT
{
    [Inline]
    public class H3DLUTSampler : ICustomSerialization, INamed
    {
        [Ignore] private float[] _Table;

        private uint[] Commands;
        [Padding (4)] public H3DLUTFlags Flags;

        public H3DLUTSampler ()
        {
            _Table = new float[256];
        }

        public float[] Table {
            get => _Table;
            set {
                if (value == null) return;

                if (value.Length != 256) return;

                _Table = value;
            }
        }

        void ICustomSerialization.Deserialize (BinaryDeserializer Deserializer)
        {
            uint Index = 0;

            var Reader = new PICACommandReader (Commands);

            while (Reader.HasCommand) {
                var Cmd = Reader.GetCommand ();

                if (Cmd.Register == PICARegister.GPUREG_LIGHTING_LUT_INDEX)
                    Index = Cmd.Parameters[0] & 0xff;
                else if (
                    Cmd.Register >= PICARegister.GPUREG_LIGHTING_LUT_DATA0 &&
                    Cmd.Register <= PICARegister.GPUREG_LIGHTING_LUT_DATA7)
                    foreach (var Param in Cmd.Parameters)
                        _Table[Index++] = (Param & 0xfff) / (float) 0xfff;
            }
        }

        bool ICustomSerialization.Serialize (BinarySerializer Serializer)
        {
            var QuantizedValues = new uint[256];

            for (var Index = 0; Index < _Table.Length; Index++) {
                float Difference = 0;

                if (Index < _Table.Length - 1) Difference = _Table[Index + 1] - _Table[Index];

                var Value = (int) (_Table[Index] * 0xfff);
                var Diff = (int) (Difference * 0x7ff);

                QuantizedValues[Index] = (uint) (Value | (Diff << 12)) & 0xffffff;
            }

            var Writer = new PICACommandWriter ();

            Writer.SetCommands (PICARegister.GPUREG_LIGHTING_LUT_DATA0, false, 0xf, QuantizedValues);

            Writer.WriteEnd ();

            Commands = Writer.GetBuffer ();

            return false;
        }

        public string Name { get; set; }
    }
}