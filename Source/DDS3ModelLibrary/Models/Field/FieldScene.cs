using System.Collections.Generic;
using System.IO;
using System.Linq;
using DDS3ModelLibrary.IO;
using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary.Models.Field
{
    public sealed class FieldScene : FieldResource
    {
        public override ResourceDescriptor ResourceDescriptor { get; } = new ResourceDescriptor( ResourceFileType.FieldResource, ResourceIdentifier.FieldScene );

        public List<FieldObject> Objects { get; }

        public FieldSceneField1CData Field1C { get; set; }

        public FieldScene()
        {
            Objects = new List<FieldObject>();
        }

        public FieldScene( string filePath ) : this()
        {
            using ( var reader = new EndianBinaryReader( new MemoryStream( File.ReadAllBytes( filePath ) ), filePath, Endianness.Little ) )
                Read( reader );
        }

        public FieldScene( Stream stream, bool leaveOpen = false ) : this()
        {
            using ( var reader = new EndianBinaryReader( stream, leaveOpen, Endianness.Little ) )
                Read( reader );
        }

        internal override void ReadContent( EndianBinaryReader reader, IOContext context )
        {
            var objectListCount = reader.ReadInt32();
            reader.ReadOffset( () =>
            {
                for ( int i = 0; i < objectListCount; i++ )
                {
                    var list = reader.ReadObject<FieldObjectList>();
                    Objects.AddRange( list );
                }
            });
            Field1C = reader.ReadObjectOffset<FieldSceneField1CData>();
        }

        internal override void WriteContent( EndianBinaryWriter writer, IOContext context )
        {
            var objectLists = Objects.GroupBy( x => x.ResourceType ).Select( x => new FieldObjectList( x ) ).OrderBy( x => x.Type ).ToList();
            writer.Write( objectLists.Count );
            writer.ScheduleWriteListOffsetAligned( objectLists, 16 );
            writer.ScheduleWriteObjectOffsetAligned( Field1C, 16 );
        }
    }
}
