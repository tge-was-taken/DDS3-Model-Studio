using System.Collections;
using System.Collections.Generic;
using System.IO;
using DDS3ModelLibrary.IO;
using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary.Textures
{
    public sealed class TexturePack : Resource, IList<Texture>
    {
        public override ResourceDescriptor ResourceDescriptor { get; } =
            new ResourceDescriptor( ResourceFileType.TexturePack, ResourceIdentifier.TexturePack );

        public List<Texture> Textures { get; }

        public TexturePack()
        {
            Textures = new List<Texture>();
        }

        public TexturePack( string filePath ) : this()
        {
            using ( var reader = new EndianBinaryReader( new MemoryStream( File.ReadAllBytes( filePath ) ), filePath, Endianness.Little ) )
                Read( reader );
        }

        public TexturePack( Stream stream, bool leaveOpen = false ) : this()
        {
            using ( var reader = new EndianBinaryReader( stream, leaveOpen, Endianness.Little ) )
                Read( reader );
        }

        internal override void ReadContent( EndianBinaryReader reader, IOContext context )
        {
            var textureCount = reader.ReadInt32();
            long nextTextureOffset = 0;
            for ( int i = 0; i < textureCount; i++ )
            {
                // Some files have broken offsets (f021_aljira.PB), so we must calculate them ourselves
                var offset = reader.ReadInt32();
                var nextOffset = reader.Position;

                if ( i == 0 )
                {
                    reader.SeekBegin( offset + reader.BaseOffset );
                }
                else
                {
                    reader.SeekBegin( nextTextureOffset );
                }

                Textures.Add( reader.ReadObject<Texture>() );
                reader.Align( 64 );

                if ( ( i + 1 ) != textureCount )
                {
                    nextTextureOffset = reader.Position;
                    reader.SeekBegin( nextOffset );
                }
            }

            // Make sure we end at the end of the last texture
        }

        internal override void WriteContent( EndianBinaryWriter writer, IOContext context )
        {
            writer.Write( Textures.Count );
            foreach ( var texture in Textures )
                writer.ScheduleWriteObjectOffsetAligned( texture, 64 );

            writer.PerformScheduledWrites();
        }

        #region IList

        public int Count => ( ( IList<Texture> )Textures ).Count;

        public bool IsReadOnly => ( ( IList<Texture> )Textures ).IsReadOnly;

        public Texture this[int index] { get => ( ( IList<Texture> )Textures )[index]; set => ( ( IList<Texture> )Textures )[index] = value; }


        public int IndexOf( Texture item )
        {
            return ( ( IList<Texture> )Textures ).IndexOf( item );
        }

        public void Insert( int index, Texture item )
        {
            ( ( IList<Texture> )Textures ).Insert( index, item );
        }

        public void RemoveAt( int index )
        {
            ( ( IList<Texture> )Textures ).RemoveAt( index );
        }

        public void Add( Texture item )
        {
            ( ( IList<Texture> )Textures ).Add( item );
        }

        public void Clear()
        {
            ( ( IList<Texture> )Textures ).Clear();
        }

        public bool Contains( Texture item )
        {
            return ( ( IList<Texture> )Textures ).Contains( item );
        }

        public void CopyTo( Texture[] array, int arrayIndex )
        {
            ( ( IList<Texture> )Textures ).CopyTo( array, arrayIndex );
        }

        public bool Remove( Texture item )
        {
            return ( ( IList<Texture> )Textures ).Remove( item );
        }

        public IEnumerator<Texture> GetEnumerator()
        {
            return ( ( IList<Texture> )Textures ).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ( ( IList<Texture> )Textures ).GetEnumerator();
        }

        #endregion
    }
}
