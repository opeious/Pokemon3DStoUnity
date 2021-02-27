using System.IO;

namespace P3DS2U.Editor.SPICA.GFL2.Model
{
    public struct GFHashName
    {
        public uint Hash;
        public string Name;

        public GFHashName (string Name)
        {
            var FNV = new GFNV1 ();

            FNV.Hash (Name);

            Hash = FNV.HashCode;

            this.Name = Name;
        }

        public GFHashName (BinaryReader Reader)
        {
            Hash = Reader.ReadUInt32 ();
            Name = Reader.ReadString ();
        }

        public void Write (BinaryWriter Writer)
        {
            Writer.Write (Hash);
            Writer.Write (Name);
        }
    }
}