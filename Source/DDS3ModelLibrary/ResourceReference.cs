using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDS3ModelLibrary
{
    public struct ResourceReference
    {
        public int Index { get; set; }

        public string Name { get; set; }

        public ResourceReference( int index )
        {
            Index = index;
            Name = null;
        }

        public ResourceReference( int index, string name )
        {
            Index = index;
            Name = name;
        }

        public static implicit operator int( ResourceReference value ) => value.Index;
        public static implicit operator ResourceReference( int value ) => new ResourceReference( value );
    }
}
