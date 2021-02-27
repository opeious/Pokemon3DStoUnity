namespace P3DS2U.Editor.SPICA.Serialization
{
    internal struct SerializationOptions
    {
        public LengthPos LenPos;
        public PointerType PtrType;

        public SerializationOptions (LengthPos LenPos, PointerType PtrType)
        {
            this.LenPos = LenPos;
            this.PtrType = PtrType;
        }
    }
}