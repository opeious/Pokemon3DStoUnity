namespace P3DS2U.Editor.SPICA.Commands
{
    public struct PICAColorOperation
    {
        public PICAFragOpMode FragOpMode;
        public PICABlendMode BlendMode;

        public PICAColorOperation (uint Param)
        {
            FragOpMode = (PICAFragOpMode) ((Param >> 0) & 3);
            BlendMode = (PICABlendMode) ((Param >> 8) & 1);
        }

        public uint ToUInt32 ()
        {
            var Param = 0xe4u << 16;

            Param |= ((uint) FragOpMode & 3) << 0;
            Param |= ((uint) BlendMode & 1) << 8;

            return Param;
        }
    }
}