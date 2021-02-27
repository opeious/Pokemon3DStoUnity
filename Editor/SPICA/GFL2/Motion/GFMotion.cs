using System.Collections.Generic;
using System.IO;
using System.Numerics;
using P3DS2U.Editor.SPICA.GFL2.Model;
using P3DS2U.Editor.SPICA.H3D;
using P3DS2U.Editor.SPICA.H3D.Animation;
using P3DS2U.Editor.SPICA.H3D.Model;
using P3DS2U.Editor.SPICA.Math3D;

namespace P3DS2U.Editor.SPICA.GFL2.Motion
{
    public class GFMotion
    {
        public Vector3 AnimRegionMax;

        public Vector3 AnimRegionMin;

        public uint FramesCount;

        public int Index;
        public bool IsBlended;

        public bool IsLooping;
        public GFMaterialMot MaterialAnimation;

        public GFSkeletonMot SkeletalAnimation;
        public GFVisibilityMot VisibilityAnimation;

        public GFMotion ()
        {
        }

        public GFMotion (BinaryReader Reader, int Index)
        {
            this.Index = Index;

            var Position = Reader.BaseStream.Position;

            var MagicNumber = Reader.ReadUInt32 ();
            var SectionCount = Reader.ReadUInt32 ();

            var AnimSections = new Section[SectionCount];

            for (var Anim = 0; Anim < AnimSections.Length; Anim++)
                AnimSections[Anim] = new Section {
                    SectName = (Sect) Reader.ReadUInt32 (),

                    Length = Reader.ReadUInt32 (),
                    Address = Reader.ReadUInt32 ()
                };

            //SubHeader
            Reader.BaseStream.Seek (Position + AnimSections[0].Address, SeekOrigin.Begin);

            FramesCount = Reader.ReadUInt32 ();

            IsLooping = (Reader.ReadUInt16 () & 1) != 0;
            IsBlended = (Reader.ReadUInt16 () & 1) != 0; //Not sure

            AnimRegionMin = Reader.ReadVector3 ();
            AnimRegionMax = Reader.ReadVector3 ();

            var AnimHash = Reader.ReadUInt32 ();

            //Content
            for (var Anim = 1; Anim < AnimSections.Length; Anim++) {
                Reader.BaseStream.Seek (Position + AnimSections[Anim].Address, SeekOrigin.Begin);

                switch (AnimSections[Anim].SectName) {
                    case Sect.SkeletalAnim:
                        SkeletalAnimation = new GFSkeletonMot (Reader, FramesCount);
                        break;
                    case Sect.MaterialAnim:
                        MaterialAnimation = new GFMaterialMot (Reader, FramesCount);
                        break;
                    case Sect.VisibilityAnim:
                        VisibilityAnimation = new GFVisibilityMot (Reader, FramesCount);
                        break;
                }
            }
        }

        public H3DAnimation ToH3DSkeletalAnimation (H3DDict<H3DBone> Skeleton)
        {
            var GFSkeleton = new List<GFBone> ();

            foreach (var Bone in Skeleton)
                GFSkeleton.Add (new GFBone {
                    Name = Bone.Name,
                    Parent = Bone.ParentIndex != -1 ? Skeleton[Bone.ParentIndex].Name : string.Empty,
                    Flags = (byte) (Bone.ParentIndex == -1 ? 2 : 1), //TODO: Fix, 2 = Identity and 1 Normal bone?
                    Translation = Bone.Translation,
                    Rotation = Bone.Rotation,
                    Scale = Bone.Scale
                });

            return ToH3DSkeletalAnimation (GFSkeleton);
        }

        public H3DAnimation ToH3DSkeletalAnimation (List<GFBone> Skeleton)
        {
            return SkeletalAnimation?.ToH3DAnimation (Skeleton, this);
        }

        public H3DMaterialAnim ToH3DMaterialAnimation ()
        {
            return MaterialAnimation?.ToH3DAnimation (this);
        }

        public H3DAnimation ToH3DVisibilityAnimation ()
        {
            return VisibilityAnimation?.ToH3DAnimation (this);
        }

        private enum Sect
        {
            SubHeader = 0,
            SkeletalAnim = 1,
            MaterialAnim = 3,
            VisibilityAnim = 6
        }

        private struct Section
        {
            public Sect SectName;

            public uint Length;
            public uint Address;
        }
    }
}