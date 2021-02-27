using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace P3DS2U.Editor.SPICA.PICA
{
    internal class PICACommandWriter
    {
        private readonly List<Cmd> Commands;

        public PICACommandWriter ()
        {
            Commands = new List<Cmd> ();
        }

        public int Index => Commands.Count;

        public uint[] GetBuffer ()
        {
            var Output = new uint[Commands.Count];

            for (var i = 0; i < Output.Length; i++) Output[i] = Commands[i].ValUInt;

            return Output;
        }

        public void SetCommand (PICARegister Register, uint Param, uint Mask = 0xf)
        {
            Commands.Add (Param);
            Commands.Add ((uint) Register | (Mask << 16));
        }

        public void SetCommand (PICARegister Register, float Param, uint Mask = 0xf)
        {
            Commands.Add (Param);
            Commands.Add ((uint) Register | (Mask << 16));
        }

        public void SetCommand (PICARegister Register, bool Param, uint Mask = 0xf)
        {
            Commands.Add (Param ? 1u : 0u);
            Commands.Add ((uint) Register | (Mask << 16));
        }

        public void SetCommand (PICARegister Register, params bool[] Params)
        {
            uint Param = 0;

            for (var Bit = 0; Bit < Params.Length; Bit++)
                if (Params[Bit])
                    Param |= 1u << Bit;

            SetCommand (Register, Param, 1);
        }

        public void SetCommands (PICARegister Register, bool Consecutive, uint Mask, params uint[] Params)
        {
            Commands.Add (Params[0]);

            var ExtraParams = (((uint) Params.Length - 1) & 0x7ff) << 20;
            var ConsecutiveBit = Consecutive ? 1u << 31 : 0;

            Commands.Add ((uint) Register | (Mask << 16) | ExtraParams | ConsecutiveBit);

            for (var Index = 1; Index < Params.Length; Index++) Commands.Add (Params[Index]);

            Align ();
        }

        public void SetCommands (PICARegister Register, bool Consecutive, uint Mask, params float[] Params)
        {
            Commands.Add (Params[0]);

            var ExtraParams = (((uint) Params.Length - 1) & 0x7ff) << 20;
            var ConsecutiveBit = Consecutive ? 1u << 31 : 0;

            Commands.Add ((uint) Register | (Mask << 16) | ExtraParams | ConsecutiveBit);

            for (var Index = 1; Index < Params.Length; Index++) Commands.Add (Params[Index]);

            Align ();
        }

        public void SetCommand (PICARegister Register, bool Consecutive, params uint[] Params)
        {
            SetCommands (Register, Consecutive, 0xf, Params);
        }

        public void SetCommand (PICARegister Register, bool Consecutive, params float[] Params)
        {
            SetCommands (Register, Consecutive, 0xf, Params);
        }

        public void WriteEnd ()
        {
            //Make sure that the Buffer is aligned on a 16 bytes boundary
            if ((Index & 3) == 0) SetCommand (PICARegister.GPUREG_DUMMY, 0, 0);

            SetCommand (PICARegister.GPUREG_CMDBUF_JUMP1, true);
        }

        private void Align ()
        {
            if ((Commands.Count & 1) != 0) Commands.Add (0);
        }

        [StructLayout (LayoutKind.Explicit)]
        private struct Cmd
        {
            [FieldOffset (0)] public uint ValUInt;
            [FieldOffset (0)] public float ValFloat;

            public static implicit operator Cmd (uint Value)
            {
                return new Cmd {ValUInt = Value};
            }

            public static implicit operator Cmd (float Value)
            {
                return new Cmd {ValFloat = Value};
            }
        }
    }
}