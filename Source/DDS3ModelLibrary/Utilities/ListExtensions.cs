using System.Collections.Generic;

namespace DDS3ModelLibrary.Utilities
{
    public static class ListExtensions
    {
        public static int AddUnique<T>( this List<T> list, T value )
        {
            var index = list.IndexOf( value );
            if ( index == -1 )
            {
                index = list.Count;
                list.Add( value );
            }

            return index;
        }
    }
}
