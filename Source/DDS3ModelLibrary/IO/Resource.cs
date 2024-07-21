using DDS3ModelLibrary.IO.Common;
using System;

namespace DDS3ModelLibrary.IO
{
    public abstract class Resource : AbstractResource<Resource.IOContext>
    {
        public abstract ResourceDescriptor ResourceDescriptor { get; }

        public ushort UserId { get; set; }

        protected Resource(ushort userId)
        {
            UserId = userId;
        }

        protected Resource()
        {
        }

        protected override void Read(EndianBinaryReader reader, IOContext context = null)
        {
            if (context == null)
                context = new IOContext();

            var start = reader.Position;

            if (context.Header == null && !context.IsFieldObject)
            {
                // Read the resource header and make sure that we're reading the right type of resource
                context.Header = reader.ReadObject<ResourceHeader>();
                if (context.Header.FileType != ResourceDescriptor.FileType ||
                     context.Header.Identifier != ResourceDescriptor.Identifier)
                    throw new InvalidOperationException("Resource header does not match resource type");
            }
            else
            {
                // Account for the size of the header that was read
                start -= ResourceHeader.SIZE;
            }

            if (!context.IsFieldObject)
                reader.PushBaseOffset(start);

            var end = 0L;
            if (context.Header != null)
            {
                end = start + context.Header.FileSize;
                UserId = context.Header.UserId;
            }

            ReadContent(reader, context);

            // Some files have broken offsets & filesize in their texture pack (f021_aljira.PB)
            if (context.Header != null && context.Header.Identifier != ResourceIdentifier.TexturePack)
                reader.SeekBegin(end);

            if (!context.IsFieldObject)
                reader.PopBaseOffset();
        }

        protected override void Write(EndianBinaryWriter writer, IOContext context = null)
        {
            if (context == null)
                context = new IOContext();

            if (context.IsFieldObject)
            {
                WriteContent(writer, context);
                return;
            }

            // Skip header
            writer.PushBaseOffset();
            var start = writer.Position;
            writer.SeekCurrent(16);

            // Write resource content
            WriteContent(writer, context);

            // Calculate content size 
            var end = writer.Position;
            var size = end - start;

            // Seek back to write the header
            writer.SeekBegin(start);
            writer.Write((byte)ResourceDescriptor.FileType);
            writer.Write((byte)0);
            writer.Write(UserId);
            writer.Write((uint)size);
            writer.Write((uint)ResourceDescriptor.Identifier);
            writer.Write(0);

            // Seek back to the end and align to 64 bytes
            writer.SeekBegin(end);
            writer.Align(64);
            writer.PopBaseOffset();
        }

        internal abstract void ReadContent(EndianBinaryReader reader, IOContext context);

        internal abstract void WriteContent(EndianBinaryWriter writer, IOContext context);

        public class IOContext
        {
            public ResourceHeader Header { get; set; }
            public readonly bool IsFieldObject;
            public object Context { get; set; }

            public IOContext() { }

            public IOContext(ResourceHeader header, bool isFieldObject, object context)
            {
                Header = header;
                IsFieldObject = isFieldObject;
                Context = context;
            }

            public IOContext(object context) => Context = context;

            public IOContext(bool isFieldObject) => IsFieldObject = isFieldObject;
        }
    }
}