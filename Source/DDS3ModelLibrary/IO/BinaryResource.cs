using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary.IO
{
    public class BinaryResource : Resource
    {
        private ResourceDescriptor mResourceDescriptor;

        public override ResourceDescriptor ResourceDescriptor => mResourceDescriptor;

        public byte[] Data { get; set; }

        public BinaryResource()
        {
        }

        public BinaryResource( ResourceFileType fileType, ResourceIdentifier identifier )
        {
            mResourceDescriptor = new ResourceDescriptor( fileType, identifier );
        }

        public BinaryResource( ResourceFileType fileType, ResourceIdentifier identifier, byte[] data )
        {
            mResourceDescriptor = new ResourceDescriptor( fileType, identifier );
            Data               = data;
        }

        internal override void ReadContent( EndianBinaryReader reader, IOContext context )
        {
            mResourceDescriptor = new ResourceDescriptor( context.Header.FileType, context.Header.Identifier );
            Data = reader.ReadBytes( ( int ) ( context.Header.FileSize - ResourceHeader.SIZE ) );
        }

        internal override void WriteContent( EndianBinaryWriter writer, IOContext context )
        {
            writer.Write( Data );
        }
    }
}