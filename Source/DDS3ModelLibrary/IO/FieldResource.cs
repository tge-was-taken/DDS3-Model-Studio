using DDS3ModelLibrary.IO.Common;
using DDS3ModelLibrary.IO.Internal;
using System;
using System.Linq;

namespace DDS3ModelLibrary.IO
{
    public abstract class FieldResource : AbstractResource<FieldResource.IOContext>
    {
        public abstract ResourceDescriptor ResourceDescriptor { get; }

        protected FieldResource()
        {
        }

        protected override void Read(EndianBinaryReader reader, IOContext context = null)
        {
            if (context == null)
                context = new IOContext();

            var start = reader.Position;

            if (context.Header == null)
            {
                if (reader.Position + FieldResourceHeader.SIZE >= reader.BaseStream.Length)
                    throw new InvalidOperationException("File is too small");

                // Read the resource header and make sure that we're reading the right type of resource
                context.Header = reader.ReadObject<FieldResourceHeader>();
                if ( /*  header.FileType != ResourceDescriptor.FileType || */
                    context.Header.Identifier != ResourceDescriptor.Identifier)
                    throw new InvalidOperationException("Resource header does not match resource type");
            }
            else
            {
                // Account for the size of the header that was read
                start -= FieldResourceHeader.SIZE;
            }

            reader.PushBaseOffset(start);

            var end = start + context.Header.DataSize + context.Header.RelocationTableSize;
            ReadContent(reader, context);
            reader.SeekBegin(end);
            reader.PopBaseOffset();
        }

        protected override void Write(EndianBinaryWriter writer, IOContext context = null)
        {
            // Skip header
            writer.PushBaseOffset();
            var start = writer.Position;
            writer.Write((int)ResourceDescriptor.FileType);
            writer.Write((uint)ResourceDescriptor.Identifier);
            writer.Write(0); // dummy data size
            writer.ScheduleWriteOffsetAligned(-1, 16, false, () =>
            {
                // Write data size
                var relocationTableStart = writer.Position;
                writer.SeekBegin(start + 8);
                writer.Write((int)relocationTableStart - start);

                // Encode & write relocation table
                writer.SeekBegin(relocationTableStart);
                var encodedRelocationTable = RelocationTableEncoding.Encode(writer.OffsetPositions.Select(x => (int)x).ToList(), (int)writer.BaseOffset);
                writer.Write(encodedRelocationTable);

                // Write relocation table size
                var relocationTableEnd = writer.Position;
                writer.SeekBegin(start + 16);
                writer.Write(encodedRelocationTable.Length);
                writer.SeekBegin(relocationTableEnd);
            });
            writer.Write(0); // dummy relocation table size

            // Write resource content
            WriteContent(writer, context);
            writer.PerformScheduledWrites();

            // Seek back to the end and align to 64 bytes
            writer.PopBaseOffset();
        }

        internal abstract void ReadContent(EndianBinaryReader reader, IOContext context);

        internal abstract void WriteContent(EndianBinaryWriter writer, IOContext context);

        public class IOContext
        {
            public FieldResourceHeader Header { get; set; }

            public IOContext()
            {
            }

            public IOContext(FieldResourceHeader header)
            {
                Header = header;
            }
        }
    }
}