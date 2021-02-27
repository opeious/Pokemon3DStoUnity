using System.Collections.Generic;

namespace P3DS2U.Editor.SPICA.H3D.Animation
{
    public class H3DAnimation : INamed
    {
        public readonly List<H3DAnimationElement> Elements;

        public H3DAnimationFlags AnimationFlags;
        public H3DAnimationType AnimationType;

        public ushort CurvesCount;

        public float FramesCount;

        public H3DMetaData MetaData;

        public H3DAnimation ()
        {
            Elements = new List<H3DAnimationElement> ();
        }

        public string Name { get; set; }
    }
}