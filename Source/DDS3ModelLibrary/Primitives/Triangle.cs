using System;

namespace DDS3ModelLibrary
{
    public struct Triangle : IEquatable<Triangle>
    {
        public ushort A;
        public ushort B;
        public ushort C;

        public Triangle( ushort[] indices )
        {
            if ( indices.Length != 3 )
                throw new ArgumentException( "Invalid number of indices for a triangle" );

            A = indices[0];
            B = indices[1];
            C = indices[2];
        }

        public Triangle( ushort a, ushort b, ushort c )
        {
            A = a;
            B = b;
            C = c;
        }

        public override bool Equals( object obj )
        {
            return obj is Triangle triangle && Equals( triangle );
        }

        public bool Equals( Triangle other )
        {
            return A == other.A && B == other.B && C == other.C;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 11;
                hash = hash * 33 + A.GetHashCode();
                hash = hash * 33 + B.GetHashCode();
                hash = hash * 33 + C.GetHashCode();

                return hash;
            }
        }
    }
}