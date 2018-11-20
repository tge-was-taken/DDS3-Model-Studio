using System;
using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary
{
    public class FieldObject : IBinarySerializable
    {
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        public int Id { get; set; }

        public FieldObjectResourceType ResourceType => Resource?.FieldObjectResourceType ?? 0;

        public string Name { get; set; }

        public int Field0C { get; set; }

        public FieldObjectTransform Transform { get; set; }

        public FieldObjectField14Data Field14 { get; set; }

        public int Field18 { get; set; }

        public int Field1C { get; set; }

        public IFieldObjectResource Resource { get; set; }

        public FieldObject()
        {
            Field14 = new FieldObjectField14Data();
        }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            Id        = reader.ReadInt32();
            var resourceType = ( FieldObjectResourceType ) reader.ReadInt32();
            Name      = reader.ReadStringOffset();
            Field0C   = reader.ReadInt32();
            Transform = reader.ReadObjectOffset<FieldObjectTransform>();
            Field14   = reader.ReadObjectOffset<FieldObjectField14Data>();
            Field18   = reader.ReadInt32();
            Field1C   = reader.ReadInt32();

            switch ( resourceType )
            {
                case FieldObjectResourceType.Model:
                    Resource = reader.ReadObjectOffset<Model>( ( (ResourceHeader )null, true ) );
                    break;

                case FieldObjectResourceType.Type3:
                    Resource = reader.ReadObjectOffset<FieldObjectResourceType3>( (( ResourceHeader )null, true) );
                    break;

                case FieldObjectResourceType.TextureListFileName:
                    Resource = reader.ReadObjectOffset<FieldTextureListFileName>( ( ( ResourceHeader ) null, true ) );
                    break;

                case FieldObjectResourceType.Effect:
                    Resource = reader.ReadObjectOffset<FieldEffect>( (( ResourceHeader )null, true) );
                    break;

                case FieldObjectResourceType.Light:
                    Resource = reader.ReadObjectOffset<FieldLight>( (( ResourceHeader )null, true) );
                    break;

                default:
                    reader.ReadInt32();
                    break;
            }
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            writer.Write( Id );
            writer.Write( ( int ) ResourceType );
            writer.ScheduleWriteStringOffset( Name, 16 );
            writer.Write( Field0C );
            writer.ScheduleWriteObjectOffset( Transform, 16 );
            writer.ScheduleWriteObjectOffset( Field14, 16 );
            writer.Write( Field18 );
            writer.Write( Field1C );
            writer.ScheduleWriteObjectOffset( Resource, 16, true );
        }
    }
}