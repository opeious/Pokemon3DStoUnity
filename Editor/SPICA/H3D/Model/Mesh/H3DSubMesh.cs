using System;
using System.IO;
using P3DS2U.Editor.SPICA.Commands;
using P3DS2U.Editor.SPICA.PICA;
using P3DS2U.Editor.SPICA.Serialization;
using P3DS2U.Editor.SPICA.Serialization.Attributes;
using P3DS2U.Editor.SPICA.Serialization.Serializer;

namespace P3DS2U.Editor.SPICA.H3D.Model.Mesh
{
    [Inline]
    public class H3DSubMesh : ICustomSerialization, ICustomSerializeCmd
    {
        [FixedLength (20)] [Inline] private ushort[] _BoneIndices;

        public ushort BoneIndicesCount;

        [Ignore] public ushort BoolUniforms;

        private uint[] Commands;

        [Ignore] public ushort[] Indices;

        [Ignore] public PICAPrimitiveMode PrimitiveMode;
        [Padding (2)] public H3DSubMeshSkinning Skinning;

        public H3DSubMesh ()
        {
            _BoneIndices = new ushort[20];
        }

        public H3DSubMesh (ushort[] Indices) : this ()
        {
            this.Indices = Indices;
        }

        public H3DSubMesh (ushort[] Indices, ushort[] BoneIndices, H3DSubMeshSkinning Skinning) : this ()
        {
            this.Indices = Indices;
            this.BoneIndices = BoneIndices;
            this.Skinning = Skinning;
        }

        public ushort[] BoneIndices {
            get => _BoneIndices;
            set {
                if (value == null) return;

                if (value.Length > 20) return;

                if (value.Length < 20)
                    Array.Copy (value, _BoneIndices, value.Length);
                else
                    _BoneIndices = value;

                BoneIndicesCount = (ushort) value.Length;
            }
        }

        public int MaxIndex {
            get {
                var Max = 0;

                foreach (var Index in Indices)
                    if (Max < Index)
                        Max = Index;

                return Max;
            }
        }

        void ICustomSerialization.Deserialize (BinaryDeserializer Deserializer)
        {
            var Reader = new PICACommandReader (Commands);

            uint BufferAddress = 0;
            uint BufferCount = 0;

            while (Reader.HasCommand) {
                var Cmd = Reader.GetCommand ();

                var Param = Cmd.Parameters[0];

                switch (Cmd.Register) {
                    case PICARegister.GPUREG_VSH_BOOLUNIFORM:
                        BoolUniforms = (ushort) Param;
                        break;
                    case PICARegister.GPUREG_INDEXBUFFER_CONFIG:
                        BufferAddress = Param;
                        break;
                    case PICARegister.GPUREG_NUMVERTICES:
                        BufferCount = Param;
                        break;
                    case PICARegister.GPUREG_PRIMITIVE_CONFIG:
                        PrimitiveMode = (PICAPrimitiveMode) (Param >> 8);
                        break;
                }
            }

            var Is16BitsIdx = BufferAddress >> 31 != 0;
            var Position = Deserializer.BaseStream.Position;

            Indices = new ushort[BufferCount];

            Deserializer.BaseStream.Seek (BufferAddress & 0x7fffffff, SeekOrigin.Begin);

            for (var Index = 0; Index < BufferCount; Index++)
                if (Is16BitsIdx)
                    Indices[Index] = Deserializer.Reader.ReadUInt16 ();
                else
                    Indices[Index] = Deserializer.Reader.ReadByte ();

            Deserializer.BaseStream.Seek (Position, SeekOrigin.Begin);
        }

        bool ICustomSerialization.Serialize (BinarySerializer Serializer)
        {
            var Writer = new PICACommandWriter ();

            Writer.SetCommand (PICARegister.GPUREG_VSH_BOOLUNIFORM, BoolUniforms | 0x7fff0000u);

            Writer.SetCommand (PICARegister.GPUREG_RESTART_PRIMITIVE, true);

            Writer.SetCommand (PICARegister.GPUREG_INDEXBUFFER_CONFIG, 0);

            Writer.SetCommand (PICARegister.GPUREG_NUMVERTICES, (uint) Indices.Length);

            Writer.SetCommand (PICARegister.GPUREG_START_DRAW_FUNC0, false, 1);

            Writer.SetCommand (PICARegister.GPUREG_DRAWELEMENTS, true);

            Writer.SetCommand (PICARegister.GPUREG_START_DRAW_FUNC0, true, 1);

            Writer.SetCommand (PICARegister.GPUREG_VTX_FUNC, true);

            Writer.SetCommand (PICARegister.GPUREG_PRIMITIVE_CONFIG, (uint) PrimitiveMode << 8, 8);
            Writer.SetCommand (PICARegister.GPUREG_PRIMITIVE_CONFIG, (uint) PrimitiveMode << 8, 8);

            Writer.WriteEnd ();

            Commands = Writer.GetBuffer ();

            return false;
        }

        void ICustomSerializeCmd.SerializeCmd (BinarySerializer Serializer, object Value)
        {
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

            var Position = Serializer.BaseStream.Position + 0x10;

            H3DRelocator.AddCmdReloc (Serializer, Section, Position);

            Serializer.Sections[(uint) H3DSectionId.RawData].Values.Add (new RefValue {
                Parent = this,
                Value = Data,
                Position = Position
            });
        }
    }
}