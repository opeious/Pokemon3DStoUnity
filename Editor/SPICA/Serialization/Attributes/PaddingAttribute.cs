using System;

namespace SPICA.Serialization.Attributes
{
    internal class PaddingAttribute : Attribute
    {
        public int Size;

        public PaddingAttribute (int Size)
        {
            this.Size = Size;
        }
    }
}