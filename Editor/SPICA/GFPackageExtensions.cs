using System.IO;
using System.Text;

namespace P3DS2U.Editor.SPICA
{
    internal static class GFPackageExtensions
    {
        public static Header GetPackageHeader (Stream Input)
        {
            var Reader = new BinaryReader (Input);

            var Output = new Header ();

            Output.Magic = Encoding.ASCII.GetString (Reader.ReadBytes (2));

            var Entries = Reader.ReadUInt16 ();

            Output.Entries = new Entry[Entries];

            var Position = Input.Position;

            for (var Index = 0; Index < Entries; Index++) {
                Input.Seek (Position + Index * 4, SeekOrigin.Begin);

                var StartAddress = Reader.ReadUInt32 ();
                var EndAddress = Reader.ReadUInt32 ();

                var Length = (int) (EndAddress - StartAddress);

                Output.Entries[Index] = new Entry {
                    Address = (uint) (Position - 4) + StartAddress,
                    Length = Length
                };
            }

            return Output;
        }

        public static bool IsValidPackage (Stream Input)
        {
            var Position = Input.Position;

            var Reader = new BinaryReader (Input);

            var Result = IsValidPackage (Reader);

            Input.Seek (Position, SeekOrigin.Begin);

            return Result;
        }

        private static bool IsValidPackage (BinaryReader Reader)
        {
            if (Reader.BaseStream.Length < 0x80) return false;

            var Magic0 = Reader.ReadByte ();
            var Magic1 = Reader.ReadByte ();

            if (Magic0 < 'A' || Magic0 > 'Z' ||
                Magic1 < 'A' || Magic1 > 'Z')
                return false;

            return true;
        }

        public struct Header
        {
            public string Magic;
            public Entry[] Entries;
        }

        public struct Entry
        {
            public uint Address;
            public int Length;
        }
    }
}