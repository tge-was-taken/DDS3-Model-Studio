namespace DDS3ModelLibrary.IO
{
    public class ResourceDescriptor
    {
        public ResourceFileType FileType { get; }

        public ResourceIdentifier Identifier { get; }

        public ResourceDescriptor( ResourceFileType fileType, ResourceIdentifier identifier )
        {
            FileType   = fileType;
            Identifier = identifier;
        }
    }
}