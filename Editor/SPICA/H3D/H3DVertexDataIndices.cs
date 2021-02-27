using System.IO;
using P3DS2U.Editor.SPICA.Commands;
using P3DS2U.Editor.SPICA.Serialization;
using P3DS2U.Editor.SPICA.Serialization.Attributes;
using P3DS2U.Editor.SPICA.Serialization.Serializer;

namespace P3DS2U.Editor.SPICA.H3D
{
    public struct H3DVertexDataIndices : ICustomSerialization
    {
        private byte Type;

        public PICADrawMode DrawMode;

        private ushort Count;

        public int MaxIndex {
            get {
                var Max = 0;

                foreach (var Index in Indices)
                    if (Max < Index)
                        Max = Index;

                return Max;
            }
        }

        [Ignore] public ushort[] Indices;

        void ICustomSerialization.Deserialize (BinaryDeserializer Deserializer)
        {
            var Is16Bits = Type == 1;
            var Address = Deserializer.Reader.ReadUInt32 ();
            var Position = Deserializer.BaseStream.Position;

            Indices = new ushort[Count];

            Deserializer.BaseStream.Seek (Address, SeekOrigin.Begin);

            for (var Index = 0; Index < Count; Index++)
                Indices[Index] = Is16Bits
                    ? Deserializer.Reader.ReadUInt16 ()
                    : Deserializer.Reader.ReadByte ();

            Deserializer.BaseStream.Seek (Position, SeekOrigin.Begin);
        }

        bool ICustomSerialization.Serialize (BinarySerializer Serializer)
        {
            Serializer.Writer.Write (Type);
            Serializer.Writer.Write ((byte) DrawMode);
            Serializer.Writer.Write ((ushort) Indices.Length);

            var Section = H3DSection.RawDataIndex16;

            object Data;

            if (MaxIndex <= byte.MaxValue) {
                Section = H3DSection.RawDataIndex8;

                var Buffer = new byte[Indices.Length];

                for (var Index = 0; Index < Indices.Length; Index++) Buffer[Index] = (byte) Indices[Index];

                Data = Buffer;
            } else {
                Data = Indices;
            }

            var Position = Serializer.BaseStream.Position;

            H3DRelocator.AddCmdReloc (Serializer, Section, Position);

            Serializer.Sections[(uint) H3DSectionId.RawData].Values.Add (new RefValue {
                Parent = this,
                Position = Position,
                Value = Data
            });

            Serializer.BaseStream.Seek (4, SeekOrigin.Current);

            return true;
        }
    }
}