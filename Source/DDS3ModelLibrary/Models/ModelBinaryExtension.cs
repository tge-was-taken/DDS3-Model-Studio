using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary.Models
{
    public class ModelBinaryExtension : ModelExtension
    {
        private ModelExtensionIdentifier mIdentifier;

        public override ModelExtensionIdentifier Identifier => mIdentifier;

        public byte[] Data { get; set; }

        protected override void Read( EndianBinaryReader reader, ModelExtensionHeader header )
        {
            mIdentifier = header.Identifier;
            Data        = reader.ReadBytes( header.Size - 8 );
        }

        protected override void Write( EndianBinaryWriter writer, object context )
        {
            writer.Write( Data );
        }
    }
}