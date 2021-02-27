using System;

namespace SPICA.Serialization.Attributes
{
    [AttributeUsage (AttributeTargets.Field)]
    internal class SectionAttribute : Attribute
    {
        public uint SectionId;

        public SectionAttribute (uint SectionId)
        {
            this.SectionId = SectionId;
        }
    }
}