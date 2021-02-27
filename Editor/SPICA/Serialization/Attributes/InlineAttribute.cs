using System;

namespace SPICA.Serialization.Attributes
{
    [AttributeUsage (AttributeTargets.Field | AttributeTargets.Class)]
    internal class InlineAttribute : Attribute
    {
    }
}