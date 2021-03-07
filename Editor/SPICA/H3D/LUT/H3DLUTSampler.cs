using System;
using P3DS2U.Editor.SPICA.PICA;
using P3DS2U.Editor.SPICA.Serialization;
using P3DS2U.Editor.SPICA.Serialization.Attributes;

namespace P3DS2U.Editor.SPICA.H3D.LUT
{
    [Inline]
    public class H3DLUTSampler : ICustomSerialization, INamed
    {
        [Padding(4)] public H3DLUTFlags Flags;

        private uint[] Commands;

        private string _Name;

        public string Name
        {
            get => _Name;
            set => _Name = value ?? throw new Exception("null");
        }

        [Ignore] private float[] _Table;

        public float[] Table
        {
            get => _Table;
            set
            {
                if (value == null)
                {
                    throw new Exception("null");
                }

                if (value.Length != 256)
                {
                    throw new Exception("null");
                }

                _Table = value;
            }
        }

        public H3DLUTSampler()
        {
            _Table = new float[256];
        }

        void ICustomSerialization.Deserialize(BinaryDeserializer Deserializer)
        {
            uint Index = 0;

            PICACommandReader Reader = new PICACommandReader(Commands);

            while (Reader.HasCommand)
            {
                PICACommand Cmd = Reader.GetCommand();

                if (Cmd.Register == PICARegister.GPUREG_LIGHTING_LUT_INDEX)
                {
                    Index = Cmd.Parameters[0] & 0xff;
                }
                else if (
                    Cmd.Register >= PICARegister.GPUREG_LIGHTING_LUT_DATA0 &&
                    Cmd.Register <= PICARegister.GPUREG_LIGHTING_LUT_DATA7)
                {
                    foreach (uint Param in Cmd.Parameters)
                    {
                        _Table[Index++] = (Param & 0xfff) / (float)0xfff;
                    }
                }
            }
        }

        bool ICustomSerialization.Serialize(BinarySerializer Serializer)
        {
            uint[] QuantizedValues = new uint[256];

            for (int Index = 0; Index < _Table.Length; Index++)
            {
                float Difference = 0;

                if (Index < _Table.Length - 1)
                {
                    Difference = _Table[Index + 1] - _Table[Index];
                }

                int Value = (int)(_Table[Index] * 0xfff);
                int Diff  = (int)(Difference    * 0x7ff);

                QuantizedValues[Index] = (uint)(Value | (Diff << 12)) & 0xffffff;
            }

            PICACommandWriter Writer = new PICACommandWriter();

            Writer.SetCommands(PICARegister.GPUREG_LIGHTING_LUT_DATA0, false, 0xf, QuantizedValues);

            Writer.WriteEnd();

            Commands = Writer.GetBuffer();

            return false;
        }
    }

    public class Exceptions
    {
    }
}