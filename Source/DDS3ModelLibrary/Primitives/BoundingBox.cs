using System;
using System.Collections.Generic;
using System.Numerics;
using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary.Primitives
{
    /// <summary>
    /// Represents a box consisting of a minimum and maximum bounds vector used for calculation.
    /// </summary>
    public class BoundingBox : IEquatable<BoundingBox>, IBinarySerializable
    {
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        /// <summary>
        /// The largest vector in the bounding box.
        /// </summary>
        public Vector3 Max;

        /// <summary>
        /// The smallest vector in the bounding box.
        /// </summary>
        public Vector3 Min;

        /// <summary>
        /// Creates a new <see cref="BoundingBox"/> whose values are set to the specified values.
        /// </summary>
        /// <param name="min">The value to set the minimum bounds vector to.</param>
        /// <param name="max">The value to set the maximum bounds vector to.</param>
        public BoundingBox( Vector3 min, Vector3 max )
        {
            Min = min;
            Max = max;
        }

        public BoundingBox()
        {
        }

        /// <summary>
        /// Calculates and creates a new <see cref="BoundingBox"/> from vertices.
        /// </summary>
        /// <param name="vertices">The vertices used to calculate the components.</param>
        /// <returns>A new <see cref="BoundingBox"/> calculated form the specified vertices.</returns>
        public static BoundingBox Calculate( IEnumerable<Vector3> vertices )
        {
            Vector3 minExtent = new Vector3( float.MaxValue, float.MaxValue, float.MaxValue );
            Vector3 maxExtent = new Vector3( float.MinValue, float.MinValue, float.MinValue );
            foreach ( Vector3 vertex in vertices )
            {
                minExtent.X = Math.Min( minExtent.X, vertex.X );
                minExtent.Y = Math.Min( minExtent.Y, vertex.Y );
                minExtent.Z = Math.Min( minExtent.Z, vertex.Z );

                maxExtent.X = Math.Max( maxExtent.X, vertex.X );
                maxExtent.Y = Math.Max( maxExtent.Y, vertex.Y );
                maxExtent.Z = Math.Max( maxExtent.Z, vertex.Z );
            }

            return new BoundingBox( minExtent, maxExtent );
        }

        public override bool Equals( object obj )
        {
            if ( obj == null || obj.GetType() != typeof( BoundingBox ) )
                return false;

            return Equals( ( BoundingBox )obj );
        }

        public bool Equals( BoundingBox other )
        {
            return Min == other.Min && Max == other.Max;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 11;
                hash = hash * 33 + Min.GetHashCode();
                hash = hash * 33 + Max.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return $"[{Min.X}, {Min.Y}, {Min.Z}] [{Max.X}, {Max.Y}, {Max.Z}]";
        }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            Min = reader.ReadVector3();
            Max = reader.ReadVector3();
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            writer.Write( Min );
            writer.Write( Max );
        }
    }
}