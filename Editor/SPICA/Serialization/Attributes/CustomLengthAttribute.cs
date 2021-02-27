using System;

namespace P3DS2U.Editor.SPICA.Serialization.Attributes
{
    [AttributeUsage (AttributeTargets.Field)]
    internal class CustomLengthAttribute : Attribute
    {
        public LengthPos Pos;
        public LengthSize Size;

        public CustomLengthAttribute (LengthPos Pos, LengthSize Size)
        {
            this.Pos = Pos;
            this.Size = Size;
        }
    }
}