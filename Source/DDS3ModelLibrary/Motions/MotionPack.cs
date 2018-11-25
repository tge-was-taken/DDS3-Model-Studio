using System.Collections.Generic;
using System.Linq;
using DDS3ModelLibrary.IO;
using DDS3ModelLibrary.IO.Common;
using DDS3ModelLibrary.IO.Internal;

namespace DDS3ModelLibrary.Motions
{
    public class MotionPack : Resource
    {
        public override ResourceDescriptor ResourceDescriptor { get; } =
            new ResourceDescriptor( ResourceFileType.MotionPack, ResourceIdentifier.MotionPack );

        public short Group { get; set; }
        public short PlayGroup { get; set; }
        public short FirstMotion { get; set; }
        public short Flags { get; set; }
        public List<Motion> Motions { get; private set; }
        public List<MotionController> Controllers { get; private set; }

        public MotionPack()
        {
            Motions = new List<Motion>();
            Controllers = new List<MotionController>();
        }

        internal override void ReadContent( EndianBinaryReader reader, IOContext context )
        {
            // -- header --
            var dataSize = reader.ReadInt32();
            var relocationTableSize = reader.ReadInt32();
            Group = reader.ReadInt16();
            PlayGroup = reader.ReadInt16();
            FirstMotion = reader.ReadInt16();
            Flags = reader.ReadInt16();

            // -- motion data header --
            reader.PushBaseOffset();
            var motionCount = reader.ReadInt16();
            var controllerCount = reader.ReadInt16();
            var motionTableOffset = reader.ReadInt32();

            // -- controllers --
            Controllers = reader.ReadObjects<MotionController>( controllerCount );

            // -- motions --
            reader.ReadAtOffset( motionTableOffset, () =>
            {
                Motions = reader.ReadObjectOffsets<Motion>( motionCount, Controllers ); 
            });
        }

        internal override void WriteContent( EndianBinaryWriter writer, IOContext context )
        {
            writer.OffsetPositions.Clear();

            var start = writer.Position;

            // -- header --
            // Write relocation table last (lowest priority)
            writer.PushBaseOffset( start + 0x20 );
            writer.ScheduleWriteOffsetAligned( -1, 16, () =>
            {
                // Encode & write relocation table
                var encodedRelocationTable =
                    RelocationTableEncoding.Encode( writer.OffsetPositions.Select( x => ( int )x ).ToList(), ( int )writer.BaseOffset );
                writer.WriteBytes( encodedRelocationTable );

                var end = writer.Position;
                writer.SeekBegin( start + 4 );
                writer.WriteInt32( encodedRelocationTable.Length );
                writer.SeekBegin( end );
                writer.PopBaseOffset();
            } );
            writer.PopBaseOffset();

            writer.WriteInt32( 0 );
            writer.WriteInt16( Group );
            writer.WriteInt16( PlayGroup );
            writer.WriteInt16( FirstMotion );
            writer.WriteInt16( Flags );

            // -- motion data header --
            writer.PushBaseOffset();
            writer.WriteInt16( ( short )Motions.Count );
            writer.WriteInt16( ( short )Controllers.Count );
            writer.ScheduleWriteOffsetAligned( 16, () =>
            {
                writer.ScheduleWriteObjectOffsetsAligned( Motions, 16 );
            });

            // -- controllers --
            writer.WriteObjects( Controllers );

            // write all the things
            writer.PerformScheduledWrites();
        }
    }
}
