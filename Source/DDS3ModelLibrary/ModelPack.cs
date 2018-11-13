using System.Collections.Generic;
using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary
{
    public class ModelPack : IBinarySerializable
    {
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        public ModelPackInfo Info { get; set; }

        public TexturePack TexturePack { get; set; }

        public List<Resource> Effects { get; }

        public List<Model> Models { get; }

        public List<Resource> AnimationPacks { get; }

        public ModelPack()
        {
            Info = new ModelPackInfo();
            TexturePack = null;
            Effects = new List<Resource>();
            Models = new List<Model>();
            AnimationPacks = new List<Resource>();
        }

        public ModelPack( string filePath ) : this()
        {
            using ( var reader = new EndianBinaryReader( filePath, Endianness.Little ) )
                Read( reader );
        }

        public void Save( string filePath )
        {
            using ( var writer = new EndianBinaryWriter( filePath, Endianness.Little ) )
                Write( writer );
        }

        private void Read( EndianBinaryReader reader )
        {
            var foundEnd = false;
            while ( !foundEnd && reader.Position < reader.BaseStream.Length )
            {
                var start  = reader.Position;
                var header = reader.ReadObject<ResourceHeader>();
                var end    = AlignmentHelper.Align( start + header.FileSize, 64 );

                switch ( header.Identifier )
                {
                    case ResourceIdentifier.ModelPackInfo:
                        Info = reader.ReadObject<ModelPackInfo>( header );
                        break;

                    case ResourceIdentifier.Particle:
                    case ResourceIdentifier.Video:
                        Effects.Add( reader.ReadObject<BinaryResource>( header ) );
                        break;

                    case ResourceIdentifier.TexturePack:
                        TexturePack = reader.ReadObject<TexturePack>( header );
                        break;

                    case ResourceIdentifier.Model:
                        Models.Add( reader.ReadObject<Model>( header ) );
                        break;

                    case ResourceIdentifier.AnimationPack:
                        AnimationPacks.Add( reader.ReadObject<BinaryResource>( header ) );
                        break;

                    case ResourceIdentifier.ModelPackEnd:
                        foundEnd = true;
                        break;

                    default:
                        throw new UnexpectedDataException( $"Unexpected '{header.Identifier}' chunk in PB file" );
                }

                // Some files have broken offsets & filesize in their texture pack (f021_aljira.PB)
                if ( header.Identifier != ResourceIdentifier.TexturePack )
                    reader.SeekBegin( end );
            }
        }

        private void Write( EndianBinaryWriter writer )
        {
            if ( Info != null )
            {
                // Some files don't have this
                writer.WriteObject( Info, this );
            }

            writer.WriteObjects( Effects );

            if ( TexturePack != null )
                writer.WriteObject( TexturePack );

            writer.WriteObjects( Models );
            writer.WriteObjects( AnimationPacks );

            // write dummy end chunk
            writer.Write( ( int )ResourceFileType.ModelPackEnd );
            writer.Write( 16 );
            writer.Write( ( int )ResourceIdentifier.ModelPackEnd );
            writer.Write( 0 );
            writer.WriteAlignmentPadding( 64 );
        }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context ) => Read( reader );

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context ) => Write( writer );
    }
}
