using System.IO;

namespace P3DS2U.Editor.SPICA.GFL
{
    public class GFPackage
    {
        public byte[][] Files;
        public string Magic;

        public GFPackage (Stream Input)
        {
            var Reader = new BinaryReader (Input);

            Magic = Reader.ReadPaddedString (2);

            var Count = Reader.ReadUInt16 ();

            Files = new byte[Count][];

            for (var i = 0; i < Count; i++) {
                Input.Seek (4 + i * 4, SeekOrigin.Begin);

                var StartAddress = Reader.ReadUInt32 ();
                var EndAddress = Reader.ReadUInt32 ();

                var Length = (int) (EndAddress - StartAddress);

                Input.Seek (StartAddress, SeekOrigin.Begin);

                Files[i] = Reader.ReadBytes (Length);
            }
        }

        public H3D.H3D CreateH3DFromContent ()
        {
            var Output = new H3D.H3D ();

            foreach (var File in Files) {
                if (File.Length < 4) continue;

                if (File[0] == 'B' &&
                    File[1] == 'C' &&
                    File[2] == 'H' &&
                    File[3] == '\0')
                    Output.Merge (H3D.H3D.Open (File));
            }

            return Output;
        }
    }
}