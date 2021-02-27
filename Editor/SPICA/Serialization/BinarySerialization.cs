using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SPICA.Serialization.Attributes;

namespace SPICA.Serialization
{
    internal class BinarySerialization
    {
        private const BindingFlags Binding =
            BindingFlags.DeclaredOnly |
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic;

        public readonly Stream BaseStream;

        public int FileVersion;

        protected SerializationOptions Options;

        public BinarySerialization (Stream BaseStream, SerializationOptions Options)
        {
            this.BaseStream = BaseStream;
            this.Options = Options;
        }

        protected IEnumerable<FieldInfo> GetFieldsSorted (Type ObjectType)
        {
            var TypeStack = new Stack<Type> ();

            do {
                TypeStack.Push (ObjectType);

                ObjectType = ObjectType.BaseType;
            } while (ObjectType != null);

            while (TypeStack.Count > 0) {
                ObjectType = TypeStack.Pop ();

                foreach (var Info in ObjectType.GetFields (Binding)) yield return Info;
            }
        }

        protected void Align (int BlockSize)
        {
            var Remainder = BaseStream.Position % BlockSize;

            if (Remainder != 0) BaseStream.Seek (BlockSize - Remainder, SeekOrigin.Current);
        }

        protected bool IsList (Type Type)
        {
            return typeof(IList).IsAssignableFrom (Type);
        }

        protected LengthPos GetLengthPos (FieldInfo Info = null)
        {
            return Info?.GetCustomAttribute<CustomLengthAttribute> ()?.Pos ?? Options.LenPos;
        }

        protected LengthSize GetLengthSize (FieldInfo Info = null)
        {
            return Info?.GetCustomAttribute<CustomLengthAttribute> ()?.Size ?? LengthSize.Integer;
        }

        protected int GetIntLengthSize (FieldInfo Info = null)
        {
            return GetLengthSize (Info) == LengthSize.Short ? 2 : 4;
        }
    }
}