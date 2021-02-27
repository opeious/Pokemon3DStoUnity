using System.Collections.Generic;
using System.Reflection;

namespace P3DS2U.Editor.SPICA.Serialization.Serializer
{
    internal class RefValue
    {
        public readonly List<RefValue> Childs;
        public bool HasLength;
        public bool HasTwoPtr;

        public FieldInfo Info;

        public object Parent;
        public uint PointerOffset;

        public long Position;
        public object Value;

        public RefValue ()
        {
            Childs = new List<RefValue> ();
        }

        public RefValue (object Value) : this ()
        {
            this.Value = Value;

            Info = null;

            Parent = null;

            Position = -1;
            HasLength = false;
            HasTwoPtr = false;
            PointerOffset = 0;
        }
    }
}