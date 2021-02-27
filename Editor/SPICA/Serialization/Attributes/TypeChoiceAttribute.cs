using System;

namespace P3DS2U.Editor.SPICA.Serialization.Attributes
{
    [AttributeUsage (AttributeTargets.Class | AttributeTargets.Field, AllowMultiple = true)]
    internal class TypeChoiceAttribute : Attribute
    {
        public Type Type;
        public uint TypeVal;

        public TypeChoiceAttribute (uint TypeVal, Type Type)
        {
            this.TypeVal = TypeVal;
            this.Type = Type;
        }
    }
}