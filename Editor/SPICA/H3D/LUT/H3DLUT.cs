using System.Collections.Generic;

namespace P3DS2U.Editor.SPICA.H3D.LUT
{
    public class H3DLUT : INamed
    {
        public readonly List<H3DLUTSampler> Samplers;

        public H3DLUT ()
        {
            Samplers = new List<H3DLUTSampler> ();
        }

        public string Name { get; set; }
    }
}