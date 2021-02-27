using System.Text;

namespace P3DS2U.Editor.SPICA
{
    public class FNV1a
    {
        private const uint OffsetBasis = 0x811c9dc5;
        private const uint Prime = 16777619;

        public FNV1a ()
        {
            HashCode = OffsetBasis;
        }

        public FNV1a (uint BaseHash)
        {
            HashCode = BaseHash;
        }

        public uint HashCode { get; private set; }

        public void Hash (byte Value)
        {
            HashCode ^= Value;
            HashCode *= Prime;
        }

        public void Hash (ushort Value)
        {
            Hash ((byte) (Value >> 0));
            Hash ((byte) (Value >> 8));
        }

        public void Hash (uint Value)
        {
            Hash ((byte) (Value >> 0));
            Hash ((byte) (Value >> 8));
            Hash ((byte) (Value >> 16));
            Hash ((byte) (Value >> 24));
        }

        public void Hash (sbyte Value)
        {
            Hash ((byte) Value);
        }

        public void Hash (short Value)
        {
            Hash ((ushort) Value);
        }

        public void Hash (int Value)
        {
            Hash ((uint) Value);
        }

        public void Hash (float Value)
        {
            Hash (IOUtils.ToUInt32 (Value));
        }

        public void Hash (string Value, bool Unicode = false)
        {
            if (Value != null) {
                var Data = Unicode
                    ? Encoding.Unicode.GetBytes (Value)
                    : Encoding.ASCII.GetBytes (Value);

                foreach (var Character in Data) Hash (Character);
            }
        }

        public void Hash (byte[] Values)
        {
            foreach (var Value in Values) Hash (Value);
        }

        public void Hash (ushort[] Values)
        {
            foreach (var Value in Values) Hash (Value);
        }

        public void Hash (uint[] Values)
        {
            foreach (var Value in Values) Hash (Value);
        }
    }
}