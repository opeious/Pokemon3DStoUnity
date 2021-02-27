namespace SPICA.Serialization
{
    internal interface ICustomSerialization
    {
        void Deserialize (BinaryDeserializer Deserializer);
        bool Serialize (BinarySerializer Serializer);
    }
}