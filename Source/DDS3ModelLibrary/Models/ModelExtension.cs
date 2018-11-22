using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary.Models
{
    public abstract class ModelExtension : IBinarySerializable
    {
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        public abstract ModelExtensionIdentifier Identifier { get; }

        protected abstract void Read( EndianBinaryReader reader, ModelExtensionHeader header );

        protected abstract void Write( EndianBinaryWriter writer, object context );

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            Read( reader, ( ModelExtensionHeader ) context );
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            Write( writer, context );
        }
    }
}