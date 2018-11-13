using System;
using System.Collections.Generic;
using System.Numerics;
using DDS3ModelLibrary.IO.Common;
using DDS3ModelLibrary.Primitives;

namespace DDS3ModelLibrary
{
    public class Node : IBinarySerializable
    {
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        public int Field00 { get; set; }

        public int Field04 { get; set; }

        public Node Parent { get; private set; }

        public Vector3 Rotation { get; set; }

        public Vector3 Position { get; set; }

        public Vector3 Scale { get; set; }

        public BoundingBox BoundingBox { get; set; }

        public Geometry Geometry { get; set; }

        public int Field48 { get; set; }

        public int Field4C { get; set; }

        public string Name { get; set; }

        public Node()
        {
            Field00     = 1;
            Field04     = 0;
            Parent      = null;
            Rotation    = Vector3.Zero;
            Position    = Vector3.Zero;
            Scale       = Vector3.One;
            BoundingBox = null;
            Geometry    = null;
            Field48     = 0;
            Field4C     = 0;
        }

        public override string ToString()
        {
            return Name;
        }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            var nodes = context as List<Node> ?? throw new InvalidOperationException( "Expected context argument to be the node list" );
            Field00 = reader.ReadInt32Expects( 1, "Node Field00 isnt 1" );
            Field04 = reader.ReadInt32Expects( 0, "Node Field04 isnt 0" );
            var index = reader.ReadInt32();

            var parentIndex = reader.ReadInt32();
            if ( parentIndex != -1 )
                Parent = nodes[ parentIndex ];

            Position = reader.ReadVector3();
            reader.SeekCurrent( 4 );
            Rotation = reader.ReadVector3();
            reader.SeekCurrent( 4 );
            Scale = reader.ReadVector3();
            reader.SeekCurrent( 4 );
            BoundingBox = reader.ReadObjectOffset<BoundingBox>();
            Geometry    = reader.ReadObjectOffset<Geometry>();
            Field48     = reader.ReadInt32Expects( 0, "Node Field48 isnt 0" );
            Field4C     = reader.ReadInt32Expects( 0, "Node Field4C isnt 0" );
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            var nodes = context as List<Node> ?? throw new InvalidOperationException( "Expected context argument to be the node list" );
            writer.Write( Field00 );
            writer.Write( Field04 );
            writer.Write( nodes.IndexOf( this ) );
            writer.Write( Parent == null ? -1 : nodes.IndexOf( Parent ) );
            writer.Write( Rotation );
            writer.Write( 0f );
            writer.Write( Position );
            writer.Write( 1f );
            writer.Write( Scale );
            writer.Write( 0f );
            writer.ScheduleWriteObjectOffset( BoundingBox, 16 );
            writer.ScheduleWriteObjectOffset( Geometry, 16 );
            writer.Write( Field48 );
            writer.Write( Field4C );
        }
    }
}