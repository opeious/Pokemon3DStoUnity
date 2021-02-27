namespace SPICA.Serialization
{
    internal interface ICustomSerializeCmd
    {
        void SerializeCmd (BinarySerializer Serializer, object Value);
    }
}