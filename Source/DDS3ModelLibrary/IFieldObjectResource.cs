using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary
{
    public interface IFieldObjectResource : IBinarySerializable
    {
        FieldObjectResourceType FieldObjectResourceType { get; }
    }
}