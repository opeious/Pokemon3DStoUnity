using System.IO;
using P3DS2U.Editor.SPICA.Commands;
using P3DS2U.Editor.SPICA.PICA;

namespace P3DS2U.Editor.SPICA.GFL2.Model
{
    public class GFLUT : INamed
    {
        private string _Name;

        private float[] _Table;
        public PICALUTType Type;

        public GFLUT ()
        {
            _Table = new float[256];
        }

        public GFLUT (BinaryReader Reader, int Length) : this ()
        {
            HashId = Reader.ReadUInt32 ();

            _Name = $"LUT_{HashId:X8}";

            Reader.BaseStream.Seek (0xc, SeekOrigin.Current);

            var Commands = new uint[Length >> 2];

            for (var i = 0; i < Commands.Length; i++) Commands[i] = Reader.ReadUInt32 ();

            uint Index = 0;

            var CmdReader = new PICACommandReader (Commands);

            while (CmdReader.HasCommand) {
                var Cmd = CmdReader.GetCommand ();

                if (Cmd.Register == PICARegister.GPUREG_LIGHTING_LUT_INDEX) {
                    Index = Cmd.Parameters[0] & 0xff;
                    Type = (PICALUTType) (Cmd.Parameters[0] >> 8);
                } else if (
                    Cmd.Register >= PICARegister.GPUREG_LIGHTING_LUT_DATA0 &&
                    Cmd.Register <= PICARegister.GPUREG_LIGHTING_LUT_DATA7) {
                    foreach (var Param in Cmd.Parameters) _Table[Index++] = (Param & 0xfff) / (float) 0xfff;
                }
            }
        }

        public float[] Table {
            get => _Table;
            set {
                if (value == null) return;

                if (value.Length != 256) return;

                _Table = value;
            }
        }

        public uint HashId { get; private set; }

        public string Name {
            get => _Name;
            set {
                _Name = value;

                if (_Name != null) {
                    var FNV = new GFNV1 ();

                    FNV.Hash (_Name);

                    HashId = FNV.HashCode;
                } else {
                    HashId = 0;
                }
            }
        }

        public void Write (BinaryWriter Writer)
        {
            Writer.Write (HashId);

            Writer.BaseStream.Seek (0xc, SeekOrigin.Current);

            var QuantizedValues = new uint[256];

            for (var Index = 0; Index < _Table.Length; Index++) {
                float Difference = 0;

                if (Index < _Table.Length - 1) Difference = _Table[Index + 1] - _Table[Index];

                var Value = (int) (_Table[Index] * 0xfff);
                var Diff = (int) (Difference * 0x7ff);

                QuantizedValues[Index] = (uint) (Value | (Diff << 12)) & 0xffffff;
            }

            var CmdWriter = new PICACommandWriter ();

            CmdWriter.SetCommand (PICARegister.GPUREG_LIGHTING_LUT_INDEX, (uint) Type << 8);
            CmdWriter.SetCommands (PICARegister.GPUREG_LIGHTING_LUT_DATA0, false, 0xf, QuantizedValues);

            CmdWriter.WriteEnd ();

            var Commands = CmdWriter.GetBuffer ();

            foreach (var Cmd in Commands) Writer.Write (Cmd);
        }
    }
}