using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary
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

        internal override void ReadContent( EndianBinaryReader reader, ResourceHeader header )
        {
            mResourceDescriptor = new ResourceDescriptor( header.FileType, header.Identifier );
            Data = reader.ReadBytes( ( int ) ( header.FileSize - ResourceHeader.SIZE ) );
        }

        internal override void WriteContent( EndianBinaryWriter writer, object context )
        {
            writer.Write( Data );
        }
    }
}