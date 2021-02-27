namespace P3DS2U.Editor.SPICA.Bitmap
{
    public enum PixelFormat
    {
        DontCare = 0,
        Undefined = 0,
        Max = 15, // 0x0000000F
        Indexed = 65536, // 0x00010000
        Gdi = 131072, // 0x00020000
        Format16bppRgb555 = 135173, // 0x00021005
        Format16bppRgb565 = 135174, // 0x00021006
        Format24bppRgb = 137224, // 0x00021808
        Format32bppRgb = 139273, // 0x00022009
        Format1bppIndexed = 196865, // 0x00030101
        Format4bppIndexed = 197634, // 0x00030402
        Format8bppIndexed = 198659, // 0x00030803
        Alpha = 262144, // 0x00040000
        Format16bppArgb1555 = 397319, // 0x00061007
        PAlpha = 524288, // 0x00080000
        Format32bppPArgb = 925707, // 0x000E200B
        Extended = 1048576, // 0x00100000
        Format16bppGrayScale = 1052676, // 0x00101004
        Format48bppRgb = 1060876, // 0x0010300C
        Format64bppPArgb = 1851406, // 0x001C400E
        Canonical = 2097152, // 0x00200000
        Format32bppArgb = 2498570, // 0x0026200A
        Format64bppArgb = 3424269 // 0x0034400D
    }
}