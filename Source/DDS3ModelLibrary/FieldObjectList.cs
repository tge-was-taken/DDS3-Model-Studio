using System.Collections;
using System.Collections.Generic;
using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary
{
    public class FieldObjectList : IBinarySerializable, IList<FieldObject>
    {
        private readonly List<FieldObject> mList;

        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        public FieldObjectType Type { get; set; }

        public FieldObjectList()
        {
            mList = new List<FieldObject>();
        }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            Type = ( FieldObjectType )reader.ReadInt32();
            var count = reader.ReadInt32();
            reader.ReadOffset( () =>
            {
                for ( int i = 0; i < count; i++ )
                    Add( reader.ReadObject<FieldObject>() );
            });
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            writer.Write( ( int ) Type );
            writer.Write( Count );
            writer.ScheduleWriteListOffset( this, 16 );
        }

        #region IList
        public IEnumerator<FieldObject> GetEnumerator()
        {
            return mList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ( ( IEnumerable ) mList ).GetEnumerator();
        }

        public void Add( FieldObject item )
        {
            mList.Add( item );
        }

        public void Clear()
        {
            mList.Clear();
        }

        public bool Contains( FieldObject item )
        {
            return mList.Contains( item );
        }

        public void CopyTo( FieldObject[] array, int arrayIndex )
        {
            mList.CopyTo( array, arrayIndex );
        }

        public bool Remove( FieldObject item )
        {
            return mList.Remove( item );
        }

        public int Count => mList.Count;

        public bool IsReadOnly => false;

        public int IndexOf( FieldObject item )
        {
            return mList.IndexOf( item );
        }

        public void Insert( int index, FieldObject item )
        {
            mList.Insert( index, item );
        }

        public void RemoveAt( int index )
        {
            mList.RemoveAt( index );
        }

        public FieldObject this[ int index ]
        {
            get => mList[ index ];
            set => mList[ index ] = value;
        }
        #endregion
    }
}