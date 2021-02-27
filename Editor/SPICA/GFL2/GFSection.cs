using System.IO;

namespace P3DS2U.Editor.SPICA.GFL2
{
    internal class GFSection
    {
        public uint Length;
        public string Magic;
        private uint Padding;

        public GFSection ()
        {
            Padding = 0xffffffff;
        }

        public GFSection (string Magic) : this ()
        {
            this.Magic = Magic;
        }

        public GFSection (string Magic, uint Length) : this ()
        {
            this.Magic = Magic;
            this.Length = Length;
        }

        public GFSection (BinaryReader Reader)
        {
            Magic = Reader.ReadPaddedString (8);
            Length = Reader.ReadUInt32 ();
            Padding = Reader.ReadUInt32 ();
        }

        public void Write (BinaryWriter Writer)
        {
            Writer.Write (Magic);
            Writer.Write (Length);
            Writer.Write (0xffffffffu);
        }

        public static void SkipPadding (Stream BaseStream)
        {
            if ((BaseStream.Position & 0xf) != 0)
                BaseStream.Seek (0x10 - (BaseStream.Position & 0xf), SeekOrigin.Current);
        }
    }
}