using System.Collections.Generic;
using System.Linq;
using SPICA.Math3D;

namespace SPICA.Formats.CtrH3D.Animation
{
    public class H3DAnimMtxTransform : H3DAnimationCurve
    {
        public readonly List<Matrix3x4> Frames;

        public H3DAnimMtxTransform ()
        {
            Frames = new List<Matrix3x4> ();
        }

        public Matrix3x4 GetTransform (int Frame)
        {
            if (Frames.Count > 0) {
                if (Frame < 0)
                    return Frames.First ();
                if (Frame >= Frames.Count)
                    return Frames.Last ();
                return Frames[Frame];
            }

            return default;
        }
    }
}