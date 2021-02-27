using System;
using P3DS2U.Editor.SPICA.Bitmap;
using P3DS2U.Editor.SPICA.Commands;
using UnityEngine; // using System.Drawing;
// using System.Drawing.Imaging;

namespace P3DS2U.Editor.SPICA.Converters
{
    internal static class TextureConverter
    {
        private static readonly int[] FmtBPP = {32, 24, 16, 16, 16, 16, 16, 8, 8, 8, 4, 4, 4, 8};

        private static readonly int[] SwizzleLUT = {
            0, 1, 8, 9, 2, 3, 10, 11,
            16, 17, 24, 25, 18, 19, 26, 27,
            4, 5, 12, 13, 6, 7, 14, 15,
            20, 21, 28, 29, 22, 23, 30, 31,
            32, 33, 40, 41, 34, 35, 42, 43,
            48, 49, 56, 57, 50, 51, 58, 59,
            36, 37, 44, 45, 38, 39, 46, 47,
            52, 53, 60, 61, 54, 55, 62, 63
        };

        public static byte[] DecodeBuffer (byte[] Input, int Width, int Height, PICATextureFormat Format)
        {
            if (Format == PICATextureFormat.ETC1 ||
                Format == PICATextureFormat.ETC1A4)
                return TextureCompression.ETC1Decompress (Input, Width, Height, Format == PICATextureFormat.ETC1A4);

            var Increment = FmtBPP[(int) Format] / 8;

            if (Increment == 0) Increment = 1;

            var Output = new byte[Width * Height * 4];

            var IOffs = 0;

            for (var TY = 0; TY < Height; TY += 8)
            for (var TX = 0; TX < Width; TX += 8)
            for (var Px = 0; Px < 64; Px++) {
                var X = SwizzleLUT[Px] & 7;
                var Y = (SwizzleLUT[Px] - X) >> 3;

                var OOffs = (TX + X + (Height - 1 - (TY + Y)) * Width) * 4;

                switch (Format) {
                    case PICATextureFormat.RGBA8:
                        Output[OOffs + 0] = Input[IOffs + 3];
                        Output[OOffs + 1] = Input[IOffs + 2];
                        Output[OOffs + 2] = Input[IOffs + 1];
                        Output[OOffs + 3] = Input[IOffs + 0];

                        break;

                    case PICATextureFormat.RGB8:
                        Output[OOffs + 0] = Input[IOffs + 2];
                        Output[OOffs + 1] = Input[IOffs + 1];
                        Output[OOffs + 2] = Input[IOffs + 0];
                        Output[OOffs + 3] = 0xff;

                        break;

                    case PICATextureFormat.RGBA5551:
                        DecodeRGBA5551 (Output, OOffs, GetUShort (Input, IOffs));

                        break;

                    case PICATextureFormat.RGB565:
                        DecodeRGB565 (Output, OOffs, GetUShort (Input, IOffs));

                        break;

                    case PICATextureFormat.RGBA4:
                        DecodeRGBA4 (Output, OOffs, GetUShort (Input, IOffs));

                        break;

                    case PICATextureFormat.LA8:
                        Output[OOffs + 0] = Input[IOffs + 1];
                        Output[OOffs + 1] = Input[IOffs + 1];
                        Output[OOffs + 2] = Input[IOffs + 1];
                        Output[OOffs + 3] = Input[IOffs + 0];

                        break;

                    case PICATextureFormat.HiLo8:
                        Output[OOffs + 0] = Input[IOffs + 1];
                        Output[OOffs + 1] = Input[IOffs + 0];
                        Output[OOffs + 2] = 0;
                        Output[OOffs + 3] = 0xff;

                        break;

                    case PICATextureFormat.L8:
                        Output[OOffs + 0] = Input[IOffs];
                        Output[OOffs + 1] = Input[IOffs];
                        Output[OOffs + 2] = Input[IOffs];
                        Output[OOffs + 3] = 0xff;

                        break;

                    case PICATextureFormat.A8:
                        Output[OOffs + 0] = 0xff;
                        Output[OOffs + 1] = 0xff;
                        Output[OOffs + 2] = 0xff;
                        Output[OOffs + 3] = Input[IOffs];

                        break;

                    case PICATextureFormat.LA4:
                        Output[OOffs + 0] = (byte) ((Input[IOffs] >> 4) | (Input[IOffs] & 0xf0));
                        Output[OOffs + 1] = (byte) ((Input[IOffs] >> 4) | (Input[IOffs] & 0xf0));
                        Output[OOffs + 2] = (byte) ((Input[IOffs] >> 4) | (Input[IOffs] & 0xf0));
                        Output[OOffs + 3] = (byte) ((Input[IOffs] << 4) | (Input[IOffs] & 0x0f));

                        break;

                    case PICATextureFormat.L4:
                        var L = (Input[IOffs >> 1] >> ((IOffs & 1) << 2)) & 0xf;

                        Output[OOffs + 0] = (byte) ((L << 4) | L);
                        Output[OOffs + 1] = (byte) ((L << 4) | L);
                        Output[OOffs + 2] = (byte) ((L << 4) | L);
                        Output[OOffs + 3] = 0xff;

                        break;

                    case PICATextureFormat.A4:
                        var A = (Input[IOffs >> 1] >> ((IOffs & 1) << 2)) & 0xf;

                        Output[OOffs + 0] = 0xff;
                        Output[OOffs + 1] = 0xff;
                        Output[OOffs + 2] = 0xff;
                        Output[OOffs + 3] = (byte) ((A << 4) | A);

                        break;
                }

                IOffs += Increment;
            }

            return Output;
        }

        private static void DecodeRGBA5551 (byte[] Buffer, int Address, ushort Value)
        {
            var R = ((Value >> 1) & 0x1f) << 3;
            var G = ((Value >> 6) & 0x1f) << 3;
            var B = ((Value >> 11) & 0x1f) << 3;

            SetColor (Buffer, Address, (Value & 1) * 0xff,
                B | (B >> 5),
                G | (G >> 5),
                R | (R >> 5));
        }

        private static void DecodeRGB565 (byte[] Buffer, int Address, ushort Value)
        {
            var R = ((Value >> 0) & 0x1f) << 3;
            var G = ((Value >> 5) & 0x3f) << 2;
            var B = ((Value >> 11) & 0x1f) << 3;

            SetColor (Buffer, Address, 0xff,
                B | (B >> 5),
                G | (G >> 6),
                R | (R >> 5));
        }

        private static void DecodeRGBA4 (byte[] Buffer, int Address, ushort Value)
        {
            var R = (Value >> 4) & 0xf;
            var G = (Value >> 8) & 0xf;
            var B = (Value >> 12) & 0xf;

            SetColor (Buffer, Address, (Value & 0xf) | (Value << 4),
                B | (B << 4),
                G | (G << 4),
                R | (R << 4));
        }

        private static void SetColor (byte[] Buffer, int Address, int A, int B, int G, int R)
        {
            Buffer[Address + 0] = (byte) B;
            Buffer[Address + 1] = (byte) G;
            Buffer[Address + 2] = (byte) R;
            Buffer[Address + 3] = (byte) A;
        }

        private static ushort GetUShort (byte[] Buffer, int Address)
        {
            return (ushort) (
                (Buffer[Address + 0] << 0) |
                (Buffer[Address + 1] << 8));
        }

        public static Bitmap.Bitmap DecodeBitmap (byte[] Input, int Width, int Height, PICATextureFormat Format)
        {
            var Buffer = DecodeBuffer (Input, Width, Height, Format);

            var Output = new byte[Buffer.Length];

            var Stride = Width * 4;

            for (var Y = 0; Y < Height; Y++) {
                var IOffs = Stride * Y;
                var OOffs = Stride * (Height - 1 - Y);

                for (var X = 0; X < Width; X++) {
                    Output[OOffs + 0] = Buffer[IOffs + 2];
                    Output[OOffs + 1] = Buffer[IOffs + 1];
                    Output[OOffs + 2] = Buffer[IOffs + 0];
                    Output[OOffs + 3] = Buffer[IOffs + 3];

                    IOffs += 4;
                    OOffs += 4;
                }
            }

            return GetBitmap (Output, Width, Height);
        }

        public static byte[] Encode (Bitmap.Bitmap Img, PICATextureFormat Format)
        {
            var Input = GetBuffer (Img);

            var Output = new byte[CalculateLength (Img.Width, Img.Height, Format)];

            var OOffs = 0;

            if (Format == PICATextureFormat.ETC1 ||
                Format == PICATextureFormat.ETC1A4)
                throw new NotImplementedException ();
            for (var TY = 0; TY < Img.Height; TY += 8)
            for (var TX = 0; TX < Img.Width; TX += 8)
            for (var Px = 0; Px < 64; Px++) {
                var X = SwizzleLUT[Px] & 7;
                var Y = (SwizzleLUT[Px] - X) >> 3;

                var IOffs = (TX + X + (TY + Y) * Img.Width) * 4;

                switch (Format) {
                    case PICATextureFormat.RGBA8:
                        Output[OOffs + 0] = Input[IOffs + 3];
                        Output[OOffs + 1] = Input[IOffs + 0];
                        Output[OOffs + 2] = Input[IOffs + 1];
                        Output[OOffs + 3] = Input[IOffs + 2];

                        OOffs += 4;

                        break;

                    default: throw new NotImplementedException ();
                }
            }

            return Output;
        }


        public static int CalculateLength (int Width, int Height, PICATextureFormat Format)
        {
            var Length = Width * Height * FmtBPP[(int) Format] / 8;

            if ((Length & 0x7f) != 0) Length = (Length & ~0x7f) + 0x80;

            return Length;
        }

        public static Bitmap.Bitmap GetBitmap (byte[] Buffer, int Width, int Height)
        {
            var Rect = new Rect (0, 0, Width, Height);

            var Img = new Bitmap.Bitmap (Width, Height, PixelFormat.Format32bppArgb);

            // BitmapData ImgData = Img.LockBits(Rect, ImageLockMode.WriteOnly, Img.PixelFormat);

            // Marshal.Copy(Buffer, 0, ImgData.Scan0, Buffer.Length);

            // Img.UnlockBits(ImgData);

            return Img;
        }

        public static byte[] GetBuffer (Bitmap.Bitmap Img)
        {
            var Rect = new Rect (0, 0, Img.Width, Img.Height);

            // BitmapData ImgData = Img.LockBits(Rect, ImageLockMode.ReadOnly, Img.PixelFormat);

            // byte[] Output = new byte[ImgData.Stride * Img.Height];

            // Marshal.Copy(ImgData.Scan0, Output, 0, Output.Length);

            // Img.UnlockBits(ImgData);

            return null;
        }
    }
}