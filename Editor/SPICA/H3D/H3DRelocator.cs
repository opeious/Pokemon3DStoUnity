using System;
using System.IO;
using SPICA.Serialization;
using SPICA.Serialization.Serializer;

namespace SPICA.Formats.CtrH3D
{
    internal class H3DRelocator
    {
        private const string PointerTooBigEx = "Pointer address {0:X8} doesn't fit on 25-bits space!";
        private readonly Stream BaseStream;
        private readonly H3DHeader Header;
        private readonly BinaryReader Reader;
        private readonly BinaryWriter Writer;

        public H3DRelocator (Stream BaseStream, H3DHeader Header)
        {
            this.BaseStream = BaseStream;
            this.Header = Header;

            Reader = new BinaryReader (BaseStream);
            Writer = new BinaryWriter (BaseStream);
        }

        public void ToAbsolute ()
        {
            var Position = BaseStream.Position;

            for (var Offset = 0; Offset < Header.RelocationLength; Offset += 4) {
                BaseStream.Seek (Header.RelocationAddress + Offset, SeekOrigin.Begin);

                var Value = Reader.ReadUInt32 ();
                var PtrAddress = Value & 0x1ffffff;

                var Target = (H3DSection) ((Value >> 25) & 0xf);
                var Source = (H3DSection) (Value >> 29);

                Target += GetLegacyRelocDiff (Target, Header.BackwardCompatibility);

                if (Target != H3DSection.Strings) PtrAddress <<= 2;

                Accumulate32 (GetAddress (Source) + PtrAddress, GetAddress (Target));
            }

            BaseStream.Seek (Position, SeekOrigin.Begin);
        }

        private uint GetAddress (H3DSection Section)
        {
            switch (Section) {
                case H3DSection.Contents: return (uint) Header.ContentsAddress;
                case H3DSection.Strings: return (uint) Header.StringsAddress;
                case H3DSection.Commands: return (uint) Header.CommandsAddress;
                case H3DSection.CommandsSrc: return (uint) Header.CommandsAddress;
                case H3DSection.RawData: return (uint) Header.RawDataAddress;
                case H3DSection.RawDataTexture: return (uint) Header.RawDataAddress;
                case H3DSection.RawDataVertex: return (uint) Header.RawDataAddress;
                case H3DSection.RawDataIndex16: return (uint) Header.RawDataAddress | (1u << 31);
                case H3DSection.RawDataIndex8: return (uint) Header.RawDataAddress;
                case H3DSection.RawExt: return (uint) Header.RawExtAddress;
                case H3DSection.RawExtTexture: return (uint) Header.RawExtAddress;
                case H3DSection.RawExtVertex: return (uint) Header.RawExtAddress;
                case H3DSection.RawExtIndex16: return (uint) Header.RawExtAddress | (1u << 31);
                case H3DSection.RawExtIndex8: return (uint) Header.RawExtAddress;
            }

            return 0;
        }

        private void Accumulate32 (uint Address, uint Value)
        {
            BaseStream.Seek (Address, SeekOrigin.Begin);

            Value += Peek32 ();

            Writer.Write (Value);
        }

        private uint Peek32 ()
        {
            var Value = Reader.ReadUInt32 ();

            BaseStream.Seek (-4, SeekOrigin.Current);

            return Value;
        }

        public void ToRelative (BinarySerializer Serializer)
        {
            foreach (var Pointer in Serializer.Pointers) {
                var Position = BaseStream.Position;

                BaseStream.Seek (Pointer, SeekOrigin.Begin);

                var TargetAddress = Peek32 ();

                var Target = GetRelocation (TargetAddress);
                var Source = GetRelocation (Pointer);

                var PointerAddress = ToRelative (Pointer, Source);

                if (Target != H3DSection.Strings) PointerAddress >>= 2;

                Writer.Write (ToRelative (TargetAddress, Target));

                Target -= GetLegacyRelocDiff (Target, Header.BackwardCompatibility);

                uint Flags;

                Flags = (uint) Target;
                Flags |= (uint) Source << 4;

                CheckPtrOvr (PointerAddress);

                BaseStream.Seek (Position, SeekOrigin.Begin);

                //Commands are written right before the command is serialized.
                //The AddCmdReloc is used for that, so we don't need to write it agian.
                //The reason for this is because Commands needs flags that depends on the data format.
                if (Source != H3DSection.Commands) Writer.Write (PointerAddress | (Flags << 25));
            }

            Header.RelocationLength = (int) (BaseStream.Length - Header.RelocationAddress);
        }

        public static void AddCmdReloc (BinarySerializer Serializer, H3DSection Target, long Pointer)
        {
            var Commands = Serializer.Sections[(uint) H3DSectionId.Commands];
            var Relocation = Serializer.Sections[(uint) H3DSectionId.Relocation];

            var PointerAddress = (uint) (Pointer - Commands.Position) >> 2;

            Target -= GetLegacyRelocDiff (Target, Serializer.FileVersion);

            uint Flags;

            Flags = (uint) Target;
            Flags |= (uint) H3DSection.Commands << 4;

            CheckPtrOvr (PointerAddress);

            Relocation.Values.Add (new RefValue (PointerAddress | (Flags << 25)));
        }

        private static int GetLegacyRelocDiff (H3DSection Section, int BC)
        {
            //The enumeration for older H3D versions was different because some sections
            //didn't exist at the time, so we need to account for that.
            //This is done returning an offset to be applied to the enumeration value,
            //when the data is deserialized, this offset is added to the value from
            //the file, and when it's serialized it is subtracted from the value computed by
            //the serializer.
            if (BC > 7 && BC < 0x21 && Section >= H3DSection.RawDataVertex)
                return -1;
            if (BC < 7 && Section >= H3DSection.CommandsSrc)
                return 1;
            return 0;
        }

        private static void CheckPtrOvr (uint PointerAddress)
        {
            if (PointerAddress > 0x1ffffff)
                //The limit for a pointer value is 25 bits, that is, 32mb addressing space.
                //Note: Since most Addresses are actually 4-byte aligned (and the lower 2 bits
                //are not stored), the actual limit is 27 bits (128 mb).
                throw new OverflowException (string.Format (PointerTooBigEx, PointerAddress));
        }

        private H3DSection GetRelocation (long Position)
        {
            if (InRange (Position, Header.ContentsAddress, Header.ContentsLength))
                return H3DSection.Contents;
            if (InRange (Position, Header.StringsAddress, Header.StringsLength))
                return H3DSection.Strings;
            if (InRange (Position, Header.CommandsAddress, Header.CommandsLength))
                return H3DSection.Commands;
            if (InRange (Position, Header.RawDataAddress, Header.RawDataLength))
                return H3DSection.RawData;
            if (InRange (Position, Header.RawExtAddress, Header.RawExtLength))
                return H3DSection.RawExt;
            throw new ArgumentOutOfRangeException ();
        }

        private uint ToRelative (long Position, H3DSection Relocation)
        {
            switch (Relocation) {
                case H3DSection.Contents: return (uint) (Position - Header.ContentsAddress);
                case H3DSection.Strings: return (uint) (Position - Header.StringsAddress);
                case H3DSection.Commands: return (uint) (Position - Header.CommandsAddress);
                case H3DSection.RawData: return (uint) (Position - Header.RawDataAddress);
                case H3DSection.RawExt: return (uint) (Position - Header.RawExtAddress);
            }

            return (uint) Position;
        }

        private bool InRange (long Position, int Start, int Length)
        {
            return Position >= Start && Position < Start + Length;
        }
    }
}