using System.IO;

namespace P3DS2U.Editor.SPICA.Serialization
{
    internal class BitWriter
    {
        private uint Bools;
        private int Index;
        private readonly BinaryWriter Writer;

        public BitWriter (BinaryWriter Writer)
        {
            this.Writer = Writer;
        }

        public void WriteBit (bool Value)
        {
            Bools |= (Value ? 1u : 0u) << Index;

            if (++Index == 32) {
                Writer.Write (Bools);

                Index = 0;
                Bools = 0;
            }
        }

        public void Flush ()
        {
            if (Index != 0) Writer.Write (Bools);
        }
    }
}