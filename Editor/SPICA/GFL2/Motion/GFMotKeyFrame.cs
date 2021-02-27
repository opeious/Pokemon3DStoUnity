using System.Collections.Generic;
using System.IO;

namespace P3DS2U.Editor.SPICA.GFL2.Motion
{
    public struct GFMotKeyFrame
    {
        public int Frame;
        public float Value;
        public float Slope;

        public GFMotKeyFrame (int Frame, float Value, float Slope)
        {
            this.Frame = Frame;
            this.Value = Value;
            this.Slope = Slope;
        }

        public static void SetList (List<GFMotKeyFrame> KeyFrames, BinaryReader Reader, uint Flags, uint FramesCount)
        {
            switch (Flags & 7) {
                case 3:
                    KeyFrames.Add (new GFMotKeyFrame (0, Reader.ReadSingle (), 0));
                    break; //Constant

                //Key Frame list
                case 4:
                case 5:
                    var KeyFramesCount = Reader.ReadUInt32 ();

                    var Frames = new int[KeyFramesCount];

                    for (var Index = 0; Index < KeyFramesCount; Index++)
                        if (FramesCount > 0xff)
                            Frames[Index] = Reader.ReadUInt16 ();
                        else
                            Frames[Index] = Reader.ReadByte ();

                    while ((Reader.BaseStream.Position & 3) != 0) Reader.ReadByte ();

                    if ((Flags & 1) != 0) {
                        //Stored as Float, 64 bits per entry
                        for (var Index = 0; Index < KeyFramesCount; Index++)
                            KeyFrames.Add (new GFMotKeyFrame {
                                Frame = Frames[Index],
                                Value = Reader.ReadSingle (),
                                Slope = Reader.ReadSingle ()
                            });
                    } else {
                        //Stored as Quantized UInt16, 32 bits per entry + 128 bits of Offsets/Scale
                        var ValueScale = Reader.ReadSingle ();
                        var ValueOffset = Reader.ReadSingle ();
                        var SlopeScale = Reader.ReadSingle ();
                        var SlopeOffset = Reader.ReadSingle ();

                        for (var Index = 0; Index < KeyFramesCount; Index++)
                            KeyFrames.Add (new GFMotKeyFrame {
                                Frame = Frames[Index],
                                Value = Reader.ReadUInt16 () / (float) 0xffff * ValueScale + ValueOffset,
                                Slope = Reader.ReadUInt16 () / (float) 0xffff * SlopeScale + SlopeOffset
                            });
                    }

                    break;
            }
        }
    }
}