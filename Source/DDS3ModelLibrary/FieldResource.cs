using System;
using System.Linq;
using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary
{
    public abstract class FieldResource : IBinarySerializable
    {
        public abstract ResourceDescriptor ResourceDescriptor { get; }

        protected FieldResource()
        {
        }

        public static FieldResource Load( string path )
        {
            return null;
        }

        public void Save( string path )
        {
            using ( var writer = new EndianBinaryWriter( path, Endianness.Little ) )
            {
                Write( writer );
            }
        }

        protected virtual void Write( EndianBinaryWriter writer, object context = null )
        {
            // Skip header
            writer.PushBaseOffset();
            var start = writer.Position;
            writer.SeekCurrent( 16 );

            // Write resource content
            WriteContent( writer, context );

            // Calculate content size 
            var end  = writer.Position;
            var size = end - start;

            // Seek back to write the header
            writer.SeekBegin( start );
            writer.Write( ( int )ResourceDescriptor.FileType );
            writer.Write( ( uint )ResourceDescriptor.Identifier );
            writer.Write( ( uint )size );
            writer.ScheduleWriteOffsetAligned( 16, () =>
            {
                // Encode & write relocation table
                var encodedRelocationTable = RelocationTableEncoding.Encode( writer.OffsetPositions.Select( x => ( int )x ).ToList(), ( int )writer.BaseOffset );
                writer.Write( encodedRelocationTable );
                var relocationTableEnd = writer.Position;
                writer.SeekBegin( start + 16 );
                writer.Write( encodedRelocationTable.Length );
                writer.SeekBegin( relocationTableEnd );
            } );

            // Seek back to the end and align to 64 bytes
            writer.SeekBegin( end );
            writer.PopBaseOffset();
        }

        internal abstract void ReadContent( EndianBinaryReader reader, FieldResourceHeader header );

        internal abstract void WriteContent( EndianBinaryWriter writer, object context );

        protected void Read( EndianBinaryReader reader )
        {
            Read( reader, null );
        }

        private void Read( EndianBinaryReader reader, FieldResourceHeader header )
        {
            var start = reader.Position;

            if ( header == null )
            {
                if ( reader.Position + FieldResourceHeader.SIZE >= reader.BaseStream.Length )
                    throw new InvalidOperationException( "File is too small" );

                // Read the resource header and make sure that we're reading the right type of resource
                header = reader.ReadObject<FieldResourceHeader>();
                if ( /*  header.FileType != ResourceDescriptor.FileType || */
                     header.Identifier != ResourceDescriptor.Identifier )
                    throw new InvalidOperationException( "Resource header does not match resource type" );
            }
            else
            {
                // Account for the size of the header that was read
                start -= FieldResourceHeader.SIZE;
            }

            reader.PushBaseOffset( start );

            var end = start + header.DataSize + header.RelocationTableSize;
            ReadContent( reader, header );
            reader.SeekBegin( end );
            reader.PopBaseOffset();
        }

        // IBinarySerializable
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        /// <summary>
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="context"><see cref="ResourceHeader"/>, if null then it will be read from the stream.</param>
        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            Read( reader, context as FieldResourceHeader );
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            Write( writer, context );
        }
    }
}