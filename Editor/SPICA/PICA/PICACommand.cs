namespace P3DS2U.Editor.SPICA.PICA
{
    internal struct PICACommand
    {
        public PICARegister Register;
        public uint[] Parameters;
        public uint Mask;
    }
}