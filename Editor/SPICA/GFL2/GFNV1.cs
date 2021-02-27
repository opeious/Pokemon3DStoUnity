using System.Text;

namespace SPICA.Formats.GFL2
{
    internal class GFNV1
    {
        private const uint Prime = 16777619;

        public GFNV1 ()
        {
            HashCode = Prime;
        }

        public uint HashCode { get; private set; }

        public void Hash (byte Value)
        {
            HashCode *= Prime;
            HashCode ^= Value;
        }

        public void Hash (string Value)
        {
            if (Value != null) {
                var Data = Encoding.ASCII.GetBytes (Value);

                foreach (var Character in Data) Hash (Character);
            }
        }
    }
}