using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary
{
    public class FieldTextureListFileName : IFieldObjectResource
    {
        FieldObjectResourceType IFieldObjectResource.FieldObjectResourceType => FieldObjectResourceType.TextureListFileName;

        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        public string FileName { get; set; }

        public FieldTextureListFileName()
        {
        }

        public FieldTextureListFileName( string fileName )
        {
        }

        public static implicit operator FieldTextureListFileName( string fileName ) => new FieldTextureListFileName( fileName );
        public static implicit operator string( FieldTextureListFileName fileName ) => fileName.FileName;

        public override string ToString()
        {
            return FileName;
        }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            FileName = reader.ReadString( StringBinaryFormat.NullTerminated );
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            writer.Write( FileName, StringBinaryFormat.NullTerminated );
        }
    }
}