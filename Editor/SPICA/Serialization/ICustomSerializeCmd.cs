namespace P3DS2U.Editor.SPICA.Serialization
{
    internal interface ICustomSerializeCmd
    {
        void SerializeCmd (BinarySerializer Serializer, object Value);
    }
}