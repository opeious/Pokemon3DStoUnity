using System.Collections.Generic;
using System.IO;

namespace P3DS2U.Editor.SPICA.GFL2.Motion
{
    public class GFMotBoolean
    {
        public readonly List<bool> Values;
        public string Name;

        public GFMotBoolean ()
        {
            Values = new List<bool> ();
        }

        public GFMotBoolean (BinaryReader Reader, string Name, int Count) : this ()
        {
            this.Name = Name;

            byte Value = 0;

            for (var Index = 0; Index < Count; Index++) {
                var Bit = Index & 7;

                if (Bit == 0) Value = Reader.ReadByte ();

                Values.Add ((Value & (1 << Bit)) != 0);
            }
        }
    }
}