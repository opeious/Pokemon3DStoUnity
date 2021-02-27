using System.IO;
using SPICA.Serialization;
using SPICA.Serialization.Attributes;
using SPICA.Serialization.Serializer;

namespace SPICA.Formats.CtrH3D.Animation
{
    public class H3DAnimTransform : ICustomSerialization
    {
        private H3DAnimTransformFlags Flags;

        public H3DAnimTransform ()
        {
            ScaleX = new H3DFloatKeyFrameGroup ();
            ScaleY = new H3DFloatKeyFrameGroup ();
            ScaleZ = new H3DFloatKeyFrameGroup ();

            RotationX = new H3DFloatKeyFrameGroup ();
            RotationY = new H3DFloatKeyFrameGroup ();
            RotationZ = new H3DFloatKeyFrameGroup ();

            TranslationX = new H3DFloatKeyFrameGroup ();
            TranslationY = new H3DFloatKeyFrameGroup ();
            TranslationZ = new H3DFloatKeyFrameGroup ();
        }

        [field: Ignore] public H3DFloatKeyFrameGroup ScaleX { get; private set; }

        [field: Ignore] public H3DFloatKeyFrameGroup ScaleY { get; private set; }

        [field: Ignore] public H3DFloatKeyFrameGroup ScaleZ { get; private set; }

        [field: Ignore] public H3DFloatKeyFrameGroup RotationX { get; private set; }

        [field: Ignore] public H3DFloatKeyFrameGroup RotationY { get; private set; }

        [field: Ignore] public H3DFloatKeyFrameGroup RotationZ { get; private set; }

        [field: Ignore] public H3DFloatKeyFrameGroup TranslationX { get; private set; }

        [field: Ignore] public H3DFloatKeyFrameGroup TranslationY { get; private set; }

        [field: Ignore] public H3DFloatKeyFrameGroup TranslationZ { get; private set; }

        public bool ScaleExists => ScaleX.Exists || ScaleY.Exists || ScaleZ.Exists;

        public bool RotationExists => RotationX.Exists || RotationY.Exists || RotationZ.Exists;

        public bool TranslationExists => TranslationX.Exists || TranslationY.Exists || TranslationZ.Exists;

        void ICustomSerialization.Deserialize (BinaryDeserializer Deserializer)
        {
            var Position = Deserializer.BaseStream.Position;

            var ConstantMask = (uint) H3DAnimTransformFlags.IsScaleXConstant;
            var NotExistMask = (uint) H3DAnimTransformFlags.IsScaleXInexistent;

            for (var ElemIndex = 0; ElemIndex < 9; ElemIndex++) {
                Deserializer.BaseStream.Seek (Position, SeekOrigin.Begin);

                Position += 4;

                var Constant = ((uint) Flags & ConstantMask) != 0;
                var Exists = ((uint) Flags & NotExistMask) == 0;

                if (Exists) {
                    var FrameGrp = H3DFloatKeyFrameGroup.ReadGroup (Deserializer, Constant);

                    switch (ElemIndex) {
                        case 0:
                            ScaleX = FrameGrp;
                            break;
                        case 1:
                            ScaleY = FrameGrp;
                            break;
                        case 2:
                            ScaleZ = FrameGrp;
                            break;

                        case 3:
                            RotationX = FrameGrp;
                            break;
                        case 4:
                            RotationY = FrameGrp;
                            break;
                        case 5:
                            RotationZ = FrameGrp;
                            break;

                        case 6:
                            TranslationX = FrameGrp;
                            break;
                        case 7:
                            TranslationY = FrameGrp;
                            break;
                        case 8:
                            TranslationZ = FrameGrp;
                            break;
                    }
                }

                ConstantMask <<= 1;
                NotExistMask <<= 1;

                if (ConstantMask == (uint) H3DAnimTransformFlags.IsRotationWConstant) ConstantMask <<= 1;
            }
        }

        bool ICustomSerialization.Serialize (BinarySerializer Serializer)
        {
            var ConstantMask = (uint) H3DAnimTransformFlags.IsScaleXConstant;
            var NotExistMask = (uint) H3DAnimTransformFlags.IsScaleXInexistent;

            var Position = Serializer.BaseStream.Position;

            Flags = 0;

            Serializer.Writer.Write (0u);

            for (var ElemIndex = 0; ElemIndex < 9; ElemIndex++) {
                H3DFloatKeyFrameGroup FrameGrp = null;

                switch (ElemIndex) {
                    case 0:
                        FrameGrp = ScaleX;
                        break;
                    case 1:
                        FrameGrp = ScaleY;
                        break;
                    case 2:
                        FrameGrp = ScaleZ;
                        break;

                    case 3:
                        FrameGrp = RotationX;
                        break;
                    case 4:
                        FrameGrp = RotationY;
                        break;
                    case 5:
                        FrameGrp = RotationZ;
                        break;

                    case 6:
                        FrameGrp = TranslationX;
                        break;
                    case 7:
                        FrameGrp = TranslationY;
                        break;
                    case 8:
                        FrameGrp = TranslationZ;
                        break;
                }

                if (FrameGrp.KeyFrames.Count == 1) {
                    Flags |= (H3DAnimTransformFlags) ConstantMask;

                    Serializer.Writer.Write (FrameGrp.KeyFrames[0].Value);
                } else {
                    if (FrameGrp.KeyFrames.Count > 1)
                        Serializer.Sections[(uint) H3DSectionId.Contents].Values.Add (new RefValue {
                            Value = FrameGrp,
                            Position = Serializer.BaseStream.Position
                        });
                    else
                        Flags |= (H3DAnimTransformFlags) NotExistMask;

                    Serializer.Writer.Write (0u);
                }

                ConstantMask <<= 1;
                NotExistMask <<= 1;

                if (ConstantMask == (uint) H3DAnimTransformFlags.IsRotationWConstant) ConstantMask <<= 1;
            }

            Serializer.BaseStream.Seek (Position, SeekOrigin.Begin);

            Serializer.Writer.Write ((uint) Flags);

            Serializer.BaseStream.Seek (Position + 4 + 9 * 4, SeekOrigin.Begin);

            return true;
        }
    }
}