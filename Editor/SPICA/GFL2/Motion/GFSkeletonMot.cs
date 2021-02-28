using System.Collections.Generic;
using System.IO;
using System.Numerics;
using P3DS2U.Editor.SPICA.GFL2.Model;
using P3DS2U.Editor.SPICA.H3D.Animation;

namespace P3DS2U.Editor.SPICA.GFL2.Motion
{
    public class GFSkeletonMot
    {
        public readonly List<GFMotBoneTransform> Bones;

        public GFSkeletonMot ()
        {
            Bones = new List<GFMotBoneTransform> ();
        }

        public GFSkeletonMot (BinaryReader Reader, uint FramesCount) : this ()
        {
            var BoneNamesCount = Reader.ReadInt32 ();
            var BoneNamesLength = Reader.ReadUInt32 ();

            var Position = Reader.BaseStream.Position;

            var BoneNames = Reader.ReadStringArray (BoneNamesCount);

            Reader.BaseStream.Seek (Position + BoneNamesLength, SeekOrigin.Begin);

            foreach (var Name in BoneNames) {
                Bones.Add (new GFMotBoneTransform (Reader, Name, FramesCount));
            }
        }

        public H3DAnimation ToH3DAnimation (List<GFBone> Skeleton, GFMotion Motion)
        {
            var Output = new H3DAnimation {
                Name = "GFMotion",
                FramesCount = Motion.FramesCount,
                AnimationType = H3DAnimationType.Skeletal,
                AnimationFlags = Motion.IsLooping ? H3DAnimationFlags.IsLooping : 0
            };

            foreach (var Bone in Bones) {
                var QuatTransform = new H3DAnimQuatTransform ();

                var BoneIndex = Skeleton.FindIndex (x => x.Name == Bone.Name);

                if (BoneIndex == -1) continue;

                for (float Frame = 0; Frame < Motion.FramesCount; Frame++) {
                    var Scale = Skeleton[BoneIndex].Scale;
                    var Rotation = Skeleton[BoneIndex].Rotation;
                    var Translation = Skeleton[BoneIndex].Translation;

                    GFMotBoneTransform.SetFrameValue (Bone.ScaleX, Frame, ref Scale.X);
                    GFMotBoneTransform.SetFrameValue (Bone.ScaleY, Frame, ref Scale.Y);
                    GFMotBoneTransform.SetFrameValue (Bone.ScaleZ, Frame, ref Scale.Z);

                    GFMotBoneTransform.SetFrameValue (Bone.RotationX, Frame, ref Rotation.X);
                    GFMotBoneTransform.SetFrameValue (Bone.RotationY, Frame, ref Rotation.Y);
                    GFMotBoneTransform.SetFrameValue (Bone.RotationZ, Frame, ref Rotation.Z);

                    GFMotBoneTransform.SetFrameValue (Bone.TranslationX, Frame, ref Translation.X);
                    GFMotBoneTransform.SetFrameValue (Bone.TranslationY, Frame, ref Translation.Y);
                    GFMotBoneTransform.SetFrameValue (Bone.TranslationZ, Frame, ref Translation.Z);

                    /*
                     * gdkchan Note:
                     * When the game uses Axis Angle for rotation,
                     * I believe that the original Euler rotation can be ignored,
                     * because otherwise we would need to either convert Euler to Axis Angle or Axis to Euler,
                     * and both conversions are pretty expensive.
                     * The vector is already halved as a optimization (needs * 2).
                     */
                    Quaternion QuatRotation;

                    if (Bone.IsAxisAngle) {
                        var Angle = Rotation.Length () * 2;

                        QuatRotation = Angle > 0
                            ? Quaternion.CreateFromAxisAngle (Vector3.Normalize (Rotation), Angle)
                            : Quaternion.Identity;
                    } else {
                        QuatRotation =
                            Quaternion.CreateFromAxisAngle (Vector3.UnitZ, Rotation.Z) *
                            Quaternion.CreateFromAxisAngle (Vector3.UnitY, Rotation.Y) *
                            Quaternion.CreateFromAxisAngle (Vector3.UnitX, Rotation.X);
                    }

                    QuatTransform.Scales.Add (Scale);
                    QuatTransform.Rotations.Add (QuatRotation);
                    QuatTransform.Translations.Add (Translation);
                }

                Output.Elements.Add (new H3DAnimationElement {
                    Name = Bone.Name,
                    Content = QuatTransform,
                    TargetType = H3DTargetType.Bone,
                    PrimitiveType = H3DPrimitiveType.QuatTransform
                });
            }

            return Output;
        }
    }
}