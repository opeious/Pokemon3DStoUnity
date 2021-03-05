using System.Collections.Generic;
using System.IO;
using P3DS2U.Editor.SPICA.H3D.Animation;

namespace P3DS2U.Editor.SPICA.GFL2.Motion
{
    public class GFVisibilityMot
    {
        public readonly List<GFMotBoolean> Visibilities;

        public GFVisibilityMot ()
        {
            Visibilities = new List<GFMotBoolean> ();
        }

        public GFVisibilityMot (BinaryReader Reader, uint FramesCount) : this ()
        {
            var MeshNamesCount = Reader.ReadInt32 ();
            var MeshNamesLength = Reader.ReadUInt32 ();

            var Position = Reader.BaseStream.Position;
            
            string[] MeshNames = Reader.ReadStringArray(MeshNamesCount);

            Reader.BaseStream.Seek (Position + MeshNamesLength, SeekOrigin.Begin);

            foreach (var Name in MeshNames) {
                Visibilities.Add (new GFMotBoolean (Reader, Name, (int) (FramesCount + 1)));
            }
        }

        public H3DAnimation ToH3DAnimation (GFMotion Motion)
        {
            var Output = new H3DAnimation {
                Name = "GFMotion",
                FramesCount = Motion.FramesCount,
                AnimationType = H3DAnimationType.Visibility,
                AnimationFlags = Motion.IsLooping ? H3DAnimationFlags.IsLooping : 0
            };

            ushort Index = 0;

            foreach (var Vis in Visibilities) {
                var Anim = new H3DAnimBoolean ();

                Anim.StartFrame = 0;
                Anim.EndFrame = Motion.FramesCount;
                Anim.CurveIndex = Index++;

                foreach (var Visibility in Vis.Values) Anim.Values.Add (Visibility);

                Output.Elements.Add (new H3DAnimationElement {
                    Name = Vis.Name,
                    PrimitiveType = H3DPrimitiveType.Boolean,
                    TargetType = H3DTargetType.MeshNodeVisibility,
                    Content = Anim
                });
            }

            return Output;
        }
    }
}