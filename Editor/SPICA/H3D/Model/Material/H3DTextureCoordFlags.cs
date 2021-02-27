using System;

namespace P3DS2U.Editor.SPICA.H3D.Model.Material
{
    [Flags]
    public enum H3DTextureCoordFlags : byte
    {
        IsDirty = 1 << 0
    }
}