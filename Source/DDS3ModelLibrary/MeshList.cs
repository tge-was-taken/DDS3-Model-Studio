using System.Collections;
using System.Collections.Generic;
using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary
{
    public class MeshList : IBinarySerializable, IList<Mesh>
    {
        private readonly List<Mesh> mList;

        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        public short Field02 { get; set; }

        public MeshList()
        {
            mList = new List<Mesh>();
        }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            var meshCount = reader.ReadInt16();
            Field02 = reader.ReadInt16();

            for ( int i = 0; i < meshCount; i++ )
            {
                reader.ReadOffset( () =>
                {
                    var  meshType = ( MeshType )reader.ReadInt32();
                    Mesh mesh     = null;

                    switch ( meshType )
                    {
                        case MeshType.Type1:
                            break;
                        case MeshType.Type2:
                            break;
                        case MeshType.Type3:
                            break;
                        case MeshType.Type4:
                            break;
                        case MeshType.Type5:
                            break;
                        case MeshType.Type7:
                            mesh = reader.ReadObject<MeshType7>();
                            break;
                        case MeshType.Type8:
                            mesh = reader.ReadObject<MeshType8>();
                            break;

                        default:
                            throw new UnexpectedDataException( $"Unknown mesh type: {meshType}" );
                    }

                    if ( mesh != null )
                        Add( mesh );
                } );
            }
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            writer.Write( ( short )Count );
            writer.Write( Field02 );

            foreach ( var mesh in this )
            {
                writer.ScheduleWriteOffsetAligned( 16, () =>
                {
                    writer.Write( ( int )mesh.Type );
                    writer.WriteObject( mesh );
                } );
            }
        }

        #region IList
        public Mesh this[int index] { get => ( ( IList<Mesh> )mList )[index]; set => ( ( IList<Mesh> )mList )[index] = value; }

        public int Count => ( ( IList<Mesh> )mList ).Count;

        public bool IsReadOnly => ( ( IList<Mesh> )mList ).IsReadOnly;

        public void Add( Mesh item )
        {
            ( ( IList<Mesh> )mList ).Add( item );
        }

        public void Clear()
        {
            ( ( IList<Mesh> )mList ).Clear();
        }

        public bool Contains( Mesh item )
        {
            return ( ( IList<Mesh> )mList ).Contains( item );
        }

        public void CopyTo( Mesh[] array, int arrayIndex )
        {
            ( ( IList<Mesh> )mList ).CopyTo( array, arrayIndex );
        }

        public IEnumerator<Mesh> GetEnumerator()
        {
            return ( ( IList<Mesh> )mList ).GetEnumerator();
        }

        public int IndexOf( Mesh item )
        {
            return ( ( IList<Mesh> )mList ).IndexOf( item );
        }

        public void Insert( int index, Mesh item )
        {
            ( ( IList<Mesh> )mList ).Insert( index, item );
        }

        public bool Remove( Mesh item )
        {
            return ( ( IList<Mesh> )mList ).Remove( item );
        }

        public void RemoveAt( int index )
        {
            ( ( IList<Mesh> )mList ).RemoveAt( index );
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ( ( IList<Mesh> )mList ).GetEnumerator();
        }

        #endregion

    }
}