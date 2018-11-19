using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary
{
    public interface IFieldObjectResource : IBinarySerializable
    {
        FieldObjectType FieldObjectType { get; }
    }
}