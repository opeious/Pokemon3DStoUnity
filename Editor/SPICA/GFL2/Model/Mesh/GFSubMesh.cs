using System.Collections.Generic;
using P3DS2U.Editor.SPICA.Commands;

namespace P3DS2U.Editor.SPICA.GFL2.Model.Mesh
{
    public class GFSubMesh
    {
        public readonly List<PICAAttribute> Attributes;
        public readonly List<PICAFixedAttribute> FixedAttributes;

        public byte[] BoneIndices;

        public byte BoneIndicesCount;

        public ushort[] Indices;
        public string Name;

        public PICAPrimitiveMode PrimitiveMode;

        public byte[] RawBuffer;

        public int VertexStride;

        public GFSubMesh ()
        {
            BoneIndices = new byte[0x1f];

            Attributes = new List<PICAAttribute> ();
            FixedAttributes = new List<PICAFixedAttribute> ();
        }

        //Note: All the models observed when writing the model creation logic uses 16 bits
        //for the indices, even those where the indices are always < 256.
        //You can make this store the indices more efficiently when MaxIndex
        //of the Indices buffer is < 256.
        public bool IsIdx8Bits => false;
    }
}