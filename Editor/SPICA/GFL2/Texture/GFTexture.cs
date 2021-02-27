using System.IO;
using P3DS2U.Editor.SPICA.Commands;
using P3DS2U.Editor.SPICA.H3D.Texture;

namespace P3DS2U.Editor.SPICA.GFL2.Texture
{
    public class GFTexture
    {
        public GFTextureFormat Format;
        public ushort Height;
        public ushort MipmapSize;
        public string Name;
        public byte[] RawBuffer;

        public ushort Width;

        public GFTexture (H3DTexture tex)
        {
            Name = tex.Name;
            RawBuffer = tex.RawBuffer;
            Width = (ushort) tex.Width;
            Height = (ushort) tex.Height;
            Format = tex.Format.ToGFTextureFormat ();
            MipmapSize = tex.MipmapSize;
        }

        public GFTexture (BinaryReader Reader)
        {
            var MagicNumber = Reader.ReadUInt32 ();
            var TextureCount = Reader.ReadUInt32 ();

            var TextureSection = new GFSection (Reader);

            var TextureLength = Reader.ReadUInt32 ();

            Reader.BaseStream.Seek (0xc, SeekOrigin.Current); //Padding? Always zero it seems

            Name = Reader.ReadPaddedString (0x40);

            Width = Reader.ReadUInt16 ();
            Height = Reader.ReadUInt16 ();
            Format = (GFTextureFormat) Reader.ReadUInt16 ();
            MipmapSize = Reader.ReadUInt16 ();

            Reader.BaseStream.Seek (0x10, SeekOrigin.Current); //Padding

            RawBuffer = Reader.ReadBytes ((int) TextureLength);
        }

        public H3DTexture ToH3DTexture ()
        {
            return new H3DTexture {
                Name = Name,
                RawBufferXPos = RawBuffer,
                Width = Width,
                Height = Height,
                Format = Format.ToPICATextureFormat (),
                MipmapSize = (byte) MipmapSize
            };
        }

        public void Write (BinaryWriter Writer)
        {
            Writer.Write (0x15041213);
            Writer.Write (1);
            new GFSection ("texture", (uint) RawBuffer.Length + 0x68).Write (Writer);
            Writer.Write (RawBuffer.Length);
            Writer.WritePaddedString ("", 0x0C);
            Writer.WritePaddedString (Name, 0x40);
            Writer.Write (Width);
            Writer.Write (Height);
            Writer.Write ((short) Format);
            Writer.Write (MipmapSize);
            Writer.Write (0xFFFFFFFF);
            Writer.Write (0xFFFFFFFF);
            Writer.Write (0xFFFFFFFF);
            Writer.Write (0xFFFFFFFF);
            Writer.Write (RawBuffer);
        }
    }
}