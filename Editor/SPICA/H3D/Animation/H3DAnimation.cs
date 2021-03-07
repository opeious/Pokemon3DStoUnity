using System;
using System.Collections.Generic;

namespace P3DS2U.Editor.SPICA.H3D.Animation
{
    public class H3DAnimation : INamed
    {
        private string _Name;

        public string Name
        {
            get => _Name;
            set => _Name = value ?? throw new Exception ("Name is null");
        }

        public H3DAnimationFlags AnimationFlags;
        public H3DAnimationType  AnimationType;

        public ushort CurvesCount;

        public float FramesCount;

        public List<H3DAnimationElement> Elements {
            get { return _elements; }
        }

        public H3DMetaData MetaData;
        private readonly List<H3DAnimationElement> _elements;

        public H3DAnimation()
        {
            _elements = new List<H3DAnimationElement>();
        }
    }
}