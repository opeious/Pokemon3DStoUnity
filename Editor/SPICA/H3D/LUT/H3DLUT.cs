using System;
using System.Collections.Generic;

namespace P3DS2U.Editor.SPICA.H3D.LUT
{
    public class H3DLUT : INamed
    {
        public readonly List<H3DLUTSampler> Samplers;

        private string _Name;

        public string Name
        {
            get => _Name;
            set => _Name = value ?? throw new Exception("null");
        }

        public H3DLUT()
        {
            Samplers = new List<H3DLUTSampler>();
        }
    }
}