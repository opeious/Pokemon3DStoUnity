using System;

namespace P3DS2U.Editor.SPICA.H3D.Light
{
    [Flags]
    public enum H3DLightFlags : byte
    {
        IsTwoSidedDiffuse = 1 << 0,
        HasDistanceAttenuation = 1 << 1
    }
}