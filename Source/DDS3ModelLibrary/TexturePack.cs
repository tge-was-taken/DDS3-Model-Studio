using System.Collections;
using System.Collections.Generic;
using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary
{
    public class TexturePack : Resource, IList<Texture>
    {
        public override ResourceDescriptor ResourceDescriptor { get; } =
            new ResourceDescriptor( ResourceFileType.TexturePack, ResourceIdentifier.TexturePack );

        public List<Texture> Textures { get; }

        public TexturePack()
        {
            Textures = new List<Texture>();
        }

        public TexturePack( string path ) : this()
        {
            using ( var reader = new EndianBinaryReader( path, Endianness.Little ) )
                Read( reader );
        }

        internal override void ReadContent( EndianBinaryReader reader, ResourceHeader header )
        {
            var textureCount = reader.ReadInt32();
            for ( int i = 0; i < textureCount; i++ )
                Textures.Add( reader.ReadObjectOffset<Texture>() );
        }

        internal override void WriteContent( EndianBinaryWriter writer, object context )
        {
            writer.Write( Textures.Count );
            foreach ( var texture in Textures )
                writer.ScheduleWriteObjectOffset( texture, 64 );

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
