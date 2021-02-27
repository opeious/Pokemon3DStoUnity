namespace P3DS2U.Editor.SPICA.Serialization
{
    internal interface ICustomSerialization
    {
        void Deserialize (BinaryDeserializer Deserializer);
        bool Serialize (BinarySerializer Serializer);
    }
}