using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using DDS3ModelLibrary.IO.Common;
using DDS3ModelLibrary.Primitives;

namespace DDS3ModelLibrary
{
    public class Node : IBinarySerializable
    {
        // For debugging only, only valid when read from a file.
        private int mIndex;
        private int mParentIndex;

        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        /// <summary>
        /// Usually 1, but in hansya01.MB it is 0.
        /// </summary>
        public int Field00 { get; set; }

        public int Field04 { get; set; }

        public Node Parent { get; private set; }

        public Vector3 Rotation { get; set; }

        public Vector3 Position { get; set; }

        public Vector3 Scale { get; set; }

        public BoundingBox BoundingBox { get; set; }

        public Geometry Geometry { get; set; }

        public int Field48 { get; set; }

        /// <summary>
        /// This is only used in a handful of really old models, use the <see cref="Geometry"/> property instead.
        /// </summary>
        /// <remarks>
        /// Used in hansya01.MB and kyusyu01.MB.
        /// </remarks>
        public MeshList DeprecatedMeshList { get; set; }

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
        }

        public override string ToString()
        {
            return Name;
        }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            var nodes = context as List<Node> ?? throw new InvalidOperationException( "Expected context argument to be the node list" );
            Field00 = reader.ReadInt32();
            Field04 = reader.ReadInt32Expects( 0, "Node Field04 isnt 0" );
            mIndex = reader.ReadInt32();

            mParentIndex = reader.ReadInt32();
            if ( mParentIndex != -1 )
                Parent = nodes[ mParentIndex ];

            Rotation = reader.ReadVector3();
            reader.ReadSingleExpects( 0f, "Node Rotation W isnt 0" );
            Position = reader.ReadVector3();
            reader.ReadSingleExpects( 1f, "Node Position W isnt 1" );
            Scale = reader.ReadVector3();
            reader.ReadSingleExpects( 0f, "Node Scale W isnt 0" );
            BoundingBox = reader.ReadObjectOffset<BoundingBox>();
            Geometry    = reader.ReadObjectOffset<Geometry>();
            Field48     = reader.ReadInt32Expects( 0, "Node Field48 isnt 0" );
            DeprecatedMeshList = reader.ReadObjectOffset<MeshList>();
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            ( int index, List<Node> nodes ) = ( (int, List<Node>) ) context;
            writer.Write( Field00 );
            writer.Write( Field04 );
            writer.Write( index );
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
            writer.ScheduleWriteObjectOffset( DeprecatedMeshList, 16 );
        }
    }
}