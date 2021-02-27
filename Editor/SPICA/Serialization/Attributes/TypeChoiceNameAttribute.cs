using System;

namespace SPICA.Serialization.Attributes
{
    [AttributeUsage (AttributeTargets.Field)]
    internal class TypeChoiceNameAttribute : Attribute
    {
        public string FieldName;

        public TypeChoiceNameAttribute (string FieldName)
        {
            this.FieldName = FieldName;
        }
    }
}