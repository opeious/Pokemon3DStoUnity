using System;
using System.IO;
using UnityEngine;

namespace P3DS2U.Editor.SPICA.Converters
{
    internal static class TextureCompression
    {
        private static readonly byte[] XT = {0, 4, 0, 4};
        private static readonly byte[] YT = {0, 0, 4, 4};

        private static readonly int[,] ETC1LUT = {
            {2, 8, -2, -8},
            {5, 17, -5, -17},
            {9, 29, -9, -29},
            {13, 42, -13, -42},
            {18, 60, -18, -60},
            {24, 80, -24, -80},
            {33, 106, -33, -106},
            {47, 183, -47, -183}
        };

        public static byte[] ETC1Decompress (byte[] Input, int Width, int Height, bool Alpha)
        {
            var Output = new byte[Width * Height * 4];

            using (var MS = new MemoryStream (Input)) {
                var Reader = new BinaryReader (MS);

                for (var TY = 0; TY < Height; TY += 8)
                for (var TX = 0; TX < Width; TX += 8)
                for (var T = 0; T < 4; T++) {
                    var AlphaBlock = 0xfffffffffffffffful;

                    if (Alpha) AlphaBlock = Reader.ReadUInt64 ();

                    var ColorBlock = BitUtils.Swap64 (Reader.ReadUInt64 ());

                    var Tile = ETC1Tile (ColorBlock);

                    var TileOffset = 0;

                    for (int PY = YT[T]; PY < 4 + YT[T]; PY++)
                    for (int PX = XT[T]; PX < 4 + XT[T]; PX++) {
                        var OOffs = ((Height - 1 - (TY + PY)) * Width + TX + PX) * 4;

                        Buffer.BlockCopy (Tile, TileOffset, Output, OOffs, 3);

                        var AlphaShift = ((PX & 3) * 4 + (PY & 3)) << 2;

                        var A = (byte) ((AlphaBlock >> AlphaShift) & 0xf);

                        Output[OOffs + 3] = (byte) ((A << 4) | A);

                        TileOffset += 4;
                    }
                }

                return Output;
            }
        }

        private static byte[] ETC1Tile (ulong Block)
        {
            var BlockLow = (uint) (Block >> 32);
            var BlockHigh = (uint) (Block >> 0);

            var Flip = (BlockHigh & 0x1000000) != 0;
            var Diff = (BlockHigh & 0x2000000) != 0;

            uint R1, G1, B1;
            uint R2, G2, B2;

            if (Diff) {
                B1 = (BlockHigh & 0x0000f8) >> 0;
                G1 = (BlockHigh & 0x00f800) >> 8;
                R1 = (BlockHigh & 0xf80000) >> 16;

                B2 = (uint) ((sbyte) (B1 >> 3) + ((sbyte) ((BlockHigh & 0x000007) << 5) >> 5));
                G2 = (uint) ((sbyte) (G1 >> 3) + ((sbyte) ((BlockHigh & 0x000700) >> 3) >> 5));
                R2 = (uint) ((sbyte) (R1 >> 3) + ((sbyte) ((BlockHigh & 0x070000) >> 11) >> 5));

                B1 |= B1 >> 5;
                G1 |= G1 >> 5;
                R1 |= R1 >> 5;

                B2 = (B2 << 3) | (B2 >> 2);
                G2 = (G2 << 3) | (G2 >> 2);
                R2 = (R2 << 3) | (R2 >> 2);
            } else {
                B1 = (BlockHigh & 0x0000f0) >> 0;
                G1 = (BlockHigh & 0x00f000) >> 8;
                R1 = (BlockHigh & 0xf00000) >> 16;

                B2 = (BlockHigh & 0x00000f) << 4;
                G2 = (BlockHigh & 0x000f00) >> 4;
                R2 = (BlockHigh & 0x0f0000) >> 12;

                B1 |= B1 >> 4;
                G1 |= G1 >> 4;
                R1 |= R1 >> 4;

                B2 |= B2 >> 4;
                G2 |= G2 >> 4;
                R2 |= R2 >> 4;
            }

            var Table1 = (BlockHigh >> 29) & 7;
            var Table2 = (BlockHigh >> 26) & 7;

            var Output = new byte[4 * 4 * 4];

            if (!Flip)
                for (var Y = 0; Y < 4; Y++)
                for (var X = 0; X < 2; X++) {
                    var Color1 = ETC1Pixel (R1, G1, B1, X + 0, Y, BlockLow, Table1);
                    var Color2 = ETC1Pixel (R2, G2, B2, X + 2, Y, BlockLow, Table2);

                    var Offset1 = (Y * 4 + X) * 4;

                    Output[Offset1 + 0] = (byte) (Color1.b * 255f);
                    Output[Offset1 + 1] = (byte) (Color1.g * 255f);
                    Output[Offset1 + 2] = (byte) (Color1.r * 255f);

                    var Offset2 = (Y * 4 + X + 2) * 4;

                    Output[Offset2 + 0] = (byte) (Color2.b * 255f);
                    Output[Offset2 + 1] = (byte) (Color2.g * 255f);
                    Output[Offset2 + 2] = (byte) (Color2.r * 255f);
                }
            else
                for (var Y = 0; Y < 2; Y++)
                for (var X = 0; X < 4; X++) {
                    var Color1 = ETC1Pixel (R1, G1, B1, X, Y + 0, BlockLow, Table1);
                    var Color2 = ETC1Pixel (R2, G2, B2, X, Y + 2, BlockLow, Table2);

                    var Offset1 = (Y * 4 + X) * 4;

                    Output[Offset1 + 0] = (byte) (Color1.b * 255f);
                    Output[Offset1 + 1] = (byte) (Color1.g * 255f);
                    Output[Offset1 + 2] = (byte) (Color1.r * 255f);

                    var Offset2 = ((Y + 2) * 4 + X) * 4;

                    Output[Offset2 + 0] = (byte) (Color1.b * 255f);
                    Output[Offset2 + 1] = (byte) (Color1.g * 255f);
                    Output[Offset2 + 2] = (byte) (Color1.r * 255f);
                }

            return Output;
        }

        private static Color ETC1Pixel (uint R, uint G, uint B, int X, int Y, uint Block, uint Table)
        {
            var Index = X * 4 + Y;
            var MSB = Block << 1;

            var Pixel = Index < 8
                ? ETC1LUT[Table, ((Block >> (Index + 24)) & 1) + ((MSB >> (Index + 8)) & 2)]
                : ETC1LUT[Table, ((Block >> (Index + 8)) & 1) + ((MSB >> (Index - 8)) & 2)];

            R = Saturate ((int) (R + Pixel));
            G = Saturate ((int) (G + Pixel));
            B = Saturate ((int) (B + Pixel));

            return new Color (R, G, B, 1);
        }

        private static byte Saturate (int Value)
        {
            if (Value > byte.MaxValue) return byte.MaxValue;
            if (Value < byte.MinValue) return byte.MinValue;

            return (byte) Value;
        }
    }
}