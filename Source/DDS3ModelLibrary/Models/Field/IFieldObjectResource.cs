using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary.Models.Field
{
    public interface IFieldObjectResource : IBinarySerializable
    {
        FieldObjectResourceType FieldObjectResourceType { get; }
    }
}