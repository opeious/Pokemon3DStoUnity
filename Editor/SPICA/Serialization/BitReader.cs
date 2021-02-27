using System.IO;

namespace P3DS2U.Editor.SPICA.Serialization
{
    internal class BitReader
    {
        private uint Bools;
        private int Index;
        private readonly BinaryReader Reader;

        public BitReader (BinaryReader Reader)
        {
            this.Reader = Reader;
        }

        public bool ReadBit ()
        {
            if ((Index++ & 0x1f) == 0) {
                Bools = Reader.ReadUInt32 ();
            }

            var Value = (Bools & 1) != 0;

            Bools >>= 1;

            return Value;
        }
    }
}