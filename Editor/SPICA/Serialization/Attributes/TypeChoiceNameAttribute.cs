using System;

namespace P3DS2U.Editor.SPICA.Serialization.Attributes
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