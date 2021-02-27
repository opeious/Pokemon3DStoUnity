using System.IO;
using P3DS2U.Editor.SPICA.Serialization;
using P3DS2U.Editor.SPICA.Serialization.Serializer;

namespace P3DS2U.Editor.SPICA.H3D.Animation
{
    internal static class H3DAnimVector
    {
        public static void SetVector (BinaryDeserializer Deserializer, H3DFloatKeyFrameGroup[] Vector)
        {
            var Flags = Deserializer.Reader.ReadUInt32 ();

            var Position = Deserializer.BaseStream.Position;

            var ConstantMask = (uint) H3DAnimVectorFlags.IsXConstant;
            var NotExistMask = (uint) H3DAnimVectorFlags.IsXInexistent;

            for (var Axis = 0; Axis < Vector.Length; Axis++) {
                Deserializer.BaseStream.Seek (Position, SeekOrigin.Begin);

                Position += 4;

                var Constant = (Flags & ConstantMask) != 0;
                var Exists = (Flags & NotExistMask) == 0;

                if (Exists) Vector[Axis] = H3DFloatKeyFrameGroup.ReadGroup (Deserializer, Constant);

                ConstantMask <<= 1;
                NotExistMask <<= 1;
            }
        }

        public static void SetVector (BinaryDeserializer Deserializer, ref H3DFloatKeyFrameGroup Vector)
        {
            var Flags = (H3DAnimVectorFlags) Deserializer.Reader.ReadUInt32 ();

            var Constant = (Flags & H3DAnimVectorFlags.IsXConstant) != 0;
            var Exists = (Flags & H3DAnimVectorFlags.IsXInexistent) == 0;

            if (Exists) Vector = H3DFloatKeyFrameGroup.ReadGroup (Deserializer, Constant);
        }

        public static void WriteVector (BinarySerializer Serializer, H3DFloatKeyFrameGroup[] Vector)
        {
            var ConstantMask = (uint) H3DAnimVectorFlags.IsXConstant;
            var NotExistMask = (uint) H3DAnimVectorFlags.IsXInexistent;

            var Position = Serializer.BaseStream.Position;

            uint Flags = 0;

            Serializer.Writer.Write (0u);

            for (var ElemIndex = 0; ElemIndex < Vector.Length; ElemIndex++) {
                if (Vector[ElemIndex].KeyFrames.Count > 1) {
                    Serializer.Sections[(uint) H3DSectionId.Contents].Values.Add (new RefValue {
                        Value = Vector[ElemIndex],
                        Position = Serializer.BaseStream.Position
                    });

                    Serializer.Writer.Write (0u);
                } else if (Vector[ElemIndex].KeyFrames.Count == 0) {
                    Flags |= NotExistMask;
                    Serializer.Writer.Write (0u);
                } else {
                    Flags |= ConstantMask;
                    Serializer.Writer.Write (Vector[ElemIndex].KeyFrames[0].Value);
                }

                ConstantMask <<= 1;
                NotExistMask <<= 1;
            }

            Serializer.BaseStream.Seek (Position, SeekOrigin.Begin);

            Serializer.Writer.Write (Flags);

            Serializer.BaseStream.Seek (Position + 4 + Vector.Length * 4, SeekOrigin.Begin);
        }

        public static void WriteVector (BinarySerializer Serializer, H3DFloatKeyFrameGroup Vector)
        {
            WriteVector (Serializer, new[] {Vector});
        }
    }
}