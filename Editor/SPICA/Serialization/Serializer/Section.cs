using System;
using System.Collections.Generic;

namespace P3DS2U.Editor.SPICA.Serialization.Serializer
{
    internal class Section
    {
        public readonly List<RefValue> Values;

        public Comparison<RefValue> Comparer;

        public object Header;
        public int HeaderLength;
        public int Length;
        public int LengthWithHeader;
        public int Padding;

        public int Position;

        public Section (int Padding = 1)
        {
            Values = new List<RefValue> ();

            this.Padding = Padding;
        }

        public Section (int Padding, Comparison<RefValue> Comparer) : this (Padding)
        {
            this.Comparer = Comparer;
        }
    }
}